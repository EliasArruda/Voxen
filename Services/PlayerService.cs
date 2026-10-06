using Microsoft.JSInterop;
using Voxen.Models;
namespace Voxen.Services;
public enum PlaybackStatus { Idle, Loading, Playing, Paused, Ended, Error }
public sealed class PlayerService(IAudioSourceProvider provider, AudioProxy proxy, NativeAudioService? native = null, TimeSpan? startupTimeout = null, TimeProvider? clock = null) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<PlayerService>? _reference;
    private CancellationTokenSource? _resolution;
    private long _version;
    private bool _retryAvailable;
    private long _startupStarted;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly TimeSpan _startupTimeout = startupTimeout ?? TimeSpan.FromSeconds(20);
    private CancellationTokenSource? _startup;
    private Func<Func<Task>, Task>? _dispatch;
    public Track? CurrentTrack { get; private set; }
    public PlaybackStatus Status { get; private set; }
    public double Position { get; private set; }
    public double Duration { get; private set; }
    public double Volume { get; private set; } = .7;
    public string? Error { get; private set; }
    public bool IsSeeking { get; private set; }
    public string LoadingMessage { get; private set; } = "Conectando à fonte…";
    public Task PrepareAsync(Track track) => provider is AudioPreparationService preparation ? preparation.PrepareAsync(track) : Task.CompletedTask;
    public bool Ready => _module is not null || native?.Ready == true;
    public event Action? Changed;
    public event Func<Task>? Ended;
    public async Task InitializeAsync(IJSRuntime js, Func<Func<Task>, Task>? dispatch = null)
    {
        if (Ready) return;
        _dispatch = dispatch;
        if (native is not null)
        {
            try { await native.InitializeAsync(signal => dispatch is not null
                ? dispatch(() => OnAudioEvent(signal.Version, signal.Type, signal.Position, signal.Duration))
                : OnAudioEvent(signal.Version, signal.Type, signal.Position, signal.Duration)); await native.VolumeAsync(Volume); }
            catch (Exception) { Status = PlaybackStatus.Error; Error = "Não foi possível iniciar o áudio nativo. Use o pacote portátil com FFmpeg ou configure VOXEN_FFMPEG_PATH."; }
            Changed?.Invoke(); return;
        }
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./Scripts/audio.js");
        _reference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", _reference, Volume);
        Changed?.Invoke();
    }
    public Task PlayAsync(Track track) => PlayCoreAsync(track, false);
    private async Task PlayCoreAsync(Track track, bool retry)
    {
        if (!Ready) return;
        _retryAvailable = !retry;
        if (!retry) _startupStarted = _clock.GetTimestamp();
        var budget = _startupTimeout - _clock.GetElapsedTime(_startupStarted);
        if (budget <= TimeSpan.Zero) budget = TimeSpan.FromMilliseconds(1);
        var version = ++_version;
        _resolution?.Cancel();
        CancelStartup(); _startup = new CancellationTokenSource();
        var resolution = new CancellationTokenSource(budget, _clock);
        _resolution = resolution;
        LoadingMessage = "Conectando à fonte…";
        IsSeeking = false; CurrentTrack = track; Status = PlaybackStatus.Loading; Error = null; Position = Duration = 0;
        Changed?.Invoke();
        _ = WatchStartupAsync(version, budget, _startup.Token);
        try
        {
            if (native is not null) await native.StopAsync(version); else await _module!.InvokeVoidAsync("stop", version);
            var resource = await provider.ResolveAsync(track, resolution.Token);
            resolution.Token.ThrowIfCancellationRequested();
            if (version != _version) return;
            LoadingMessage = "Iniciando áudio…"; Changed?.Invoke();
            var url = await proxy.PublishAsync(resource, version, resolution.Token);
            if (version != _version) return;
            if (native is not null) await native.LoadAsync(resource.NativeInputUrl ?? url, version, track.Duration?.TotalSeconds ?? 0);
            else await _module!.InvokeVoidAsync("load", url, version, resource.IsHls);
        }
        catch (OperationCanceledException) when (version != _version) { }
        catch (Exception) when (version != _version) { }
        catch (Exception exception) when (version == _version)
        {
            CancelStartup();
            if (provider is AudioPreparationService preparation) preparation.Invalidate(track);
            Status = PlaybackStatus.Error;
            Error = PlaybackErrors.Describe(exception, track.Source);
            Changed?.Invoke();
        }
        finally { if (_resolution == resolution) _resolution = null; resolution.Dispose(); }
    }
    public async Task ToggleAsync()
    {
        if (!Ready) return;
        if (Status is PlaybackStatus.Error or PlaybackStatus.Ended && CurrentTrack is { } track) { await PlayAsync(track); return; }
        if (native is not null) { if (Status == PlaybackStatus.Playing) await native.PauseAsync(); else await native.ResumeAsync(); }
        else await _module!.InvokeVoidAsync(Status == PlaybackStatus.Playing ? "pause" : "resume");
    }
    public async Task StopAsync()
    {
        var version = ++_version; CancelStartup(); _resolution?.Cancel(); proxy.Clear();
        if (native is not null && native.Ready) await native.StopAsync(version);
        else if (_module is not null) await _module.InvokeVoidAsync("stop", version);
        if (version != _version) return;
        IsSeeking = false; CurrentTrack = null; Status = PlaybackStatus.Idle; Position = Duration = 0; Error = null; Changed?.Invoke();
    }
    public async Task SeekAsync(double seconds)
    {
        if (Duration <= 0 || !Ready) return;
        IsSeeking = true; Changed?.Invoke();
        try
        {
            if (native is not null) await native.SeekAsync(Math.Clamp(seconds, 0, Duration));
            else if (_module is not null) await _module.InvokeVoidAsync("seek", Math.Clamp(seconds, 0, Duration));
        }
        catch { IsSeeking = false; Changed?.Invoke(); throw; }
    }
    public async Task SetVolumeAsync(double value)
    {
        Volume = Math.Clamp(value, 0, 1);
        if (native is not null && native.Ready) await native.VolumeAsync(Volume);
        else if (_module is not null) await _module.InvokeVoidAsync("volume", Volume);
        Changed?.Invoke();
    }
    [JSInvokable]
    public async Task OnAudioEvent(long version, string type, double position, double duration)
    {
        if (version != _version) return;
        if (type == "error" && native is not null && Status == PlaybackStatus.Loading && _retryAvailable && CurrentTrack is { } retryTrack)
        {
            _retryAvailable = false;
            if (provider is AudioPreparationService cached) cached.Invalidate(retryTrack);
            await PlayCoreAsync(retryTrack, true);
            return;
        }
        if (type == "playing") _retryAvailable = false;
        if (type is "playing" or "pause" or "error" or "ended") CancelStartup();
        if (type is "seeked" or "error" or "ended") IsSeeking = false;
        Position = double.IsFinite(position) ? Math.Max(0, position) : 0;
        Duration = double.IsFinite(duration) ? Math.Max(0, duration) : 0;
        Status = type switch { "playing" => PlaybackStatus.Playing, "pause" => PlaybackStatus.Paused,
            "ended" => PlaybackStatus.Ended, "error" => PlaybackStatus.Error, _ => Status };
        if (type == "playing") Error = null;
        if (type == "error" && CurrentTrack is { } failed && provider is AudioPreparationService preparation) preparation.Invalidate(failed);
        if (type == "error") Error = "A fonte interrompeu o áudio. Tente novamente para obter um novo endereço de reprodução.";
        Changed?.Invoke();
        if (type == "ended" && Ended is { } ended) await ended();
    }
    private void CancelStartup() { _startup?.Cancel(); _startup?.Dispose(); _startup = null; }
    private async Task WatchStartupAsync(long version, TimeSpan budget, CancellationToken token)
    {
        try
        {
            await Task.Delay(budget, _clock, token);
            async Task Expire()
            {
                if (version != _version || Status != PlaybackStatus.Loading) return;
                var stoppedVersion = ++_version; _resolution?.Cancel();
                if (CurrentTrack is { } track && provider is AudioPreparationService preparation) preparation.Invalidate(track);
                Status = PlaybackStatus.Error; Error = "O áudio demorou para iniciar. Tente novamente ou escolha outra faixa."; Changed?.Invoke();
                if (native is not null) await native.StopAsync(stoppedVersion);
                else if (_module is not null) await _module.InvokeVoidAsync("stop", stoppedVersion);
            }
            if (_dispatch is not null) await _dispatch(Expire); else await Expire();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { System.Diagnostics.Trace.WriteLine("Voxen: startup timeout cleanup failed."); }
    }
    public async ValueTask DisposeAsync()
    {
        _version++; CancelStartup(); _resolution?.Cancel();
        if (_module is not null)
        {
            try { await _module.InvokeVoidAsync("dispose"); await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
        _reference?.Dispose();
    }
}
