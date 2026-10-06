using Microsoft.JSInterop;
using Voxen.Models;
namespace Voxen.Services;
public enum PlaybackStatus { Idle, Loading, Playing, Paused, Ended, Error }
public sealed class PlayerService(IAudioSourceProvider provider, AudioProxy proxy, NativeAudioService? native = null) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<PlayerService>? _reference;
    private CancellationTokenSource? _resolution;
    private long _version;
    public Track? CurrentTrack { get; private set; }
    public PlaybackStatus Status { get; private set; }
    public double Position { get; private set; }
    public double Duration { get; private set; }
    public double Volume { get; private set; } = .7;
    public string? Error { get; private set; }
    public bool Ready => _module is not null || native?.Ready == true;
    public event Action? Changed;
    public event Func<Task>? Ended;
    public async Task InitializeAsync(IJSRuntime js, Func<Func<Task>, Task>? dispatch = null)
    {
        if (Ready) return;
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
    public async Task PlayAsync(Track track)
    {
        if (!Ready) return;
        var version = ++_version;
        _resolution?.Cancel();
        var resolution = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        _resolution = resolution;
        CurrentTrack = track; Status = PlaybackStatus.Loading; Error = null; Position = Duration = 0;
        Changed?.Invoke();
        try
        {
            if (native is not null) await native.StopAsync(version); else await _module!.InvokeVoidAsync("stop", version);
            var resource = await provider.ResolveAsync(track, resolution.Token);
            resolution.Token.ThrowIfCancellationRequested();
            if (version != _version) return;
            var url = await proxy.PublishAsync(resource, version, resolution.Token);
            if (version != _version) return;
            if (native is not null) await native.LoadAsync(url, version, track.Duration?.TotalSeconds ?? 0);
            else await _module!.InvokeVoidAsync("load", url, version, resource.IsHls);
        }
        catch (OperationCanceledException) when (version != _version) { }
        catch (Exception) when (version != _version) { }
        catch (Exception) when (version == _version)
        {
            Status = PlaybackStatus.Error;
            Error = "Não foi possível reproduzir esta faixa. Tente novamente ou escolha outra.";
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
        var version = ++_version; _resolution?.Cancel(); proxy.Clear();
        if (native is not null && native.Ready) await native.StopAsync(version);
        else if (_module is not null) await _module.InvokeVoidAsync("stop", version);
        if (version != _version) return;
        CurrentTrack = null; Status = PlaybackStatus.Idle; Position = Duration = 0; Error = null; Changed?.Invoke();
    }
    public async Task SeekAsync(double seconds)
    {
        if (Duration > 0) { if (native is not null) await native.SeekAsync(Math.Clamp(seconds, 0, Duration));
            else if (_module is not null) await _module.InvokeVoidAsync("seek", Math.Clamp(seconds, 0, Duration)); }
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
        Position = double.IsFinite(position) ? Math.Max(0, position) : 0;
        Duration = double.IsFinite(duration) ? Math.Max(0, duration) : 0;
        Status = type switch { "playing" => PlaybackStatus.Playing, "pause" => PlaybackStatus.Paused,
            "ended" => PlaybackStatus.Ended, "error" => PlaybackStatus.Error, _ => Status };
        if (type == "playing") Error = null;
        if (type == "error") Error = "O áudio não iniciou. Verifique a conexão e os codecs do sistema, ou tente outra faixa.";
        Changed?.Invoke();
        if (type == "ended" && Ended is { } ended) await ended();
    }
    public async ValueTask DisposeAsync()
    {
        _version++; _resolution?.Cancel();
        if (_module is not null)
        {
            try { await _module.InvokeVoidAsync("dispose"); await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
        _reference?.Dispose();
    }
}
