using Microsoft.JSInterop;
using Voxen.Models;
namespace Voxen.Services;
public enum PlaybackStatus { Idle, Loading, Playing, Paused, Ended, Error }
public sealed class PlayerService(IAudioSourceProvider provider, AudioProxy proxy) : IAsyncDisposable
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
    public bool Ready => _module is not null;
    public event Action? Changed;
    public event Func<Task>? Ended;
    public async Task InitializeAsync(IJSRuntime js)
    {
        if (_module is not null) return;
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./Scripts/audio.js");
        _reference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", _reference, Volume);
        Changed?.Invoke();
    }
    public async Task PlayAsync(Track track)
    {
        if (_module is null) return;
        var version = ++_version;
        _resolution?.Cancel();
        var resolution = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        _resolution = resolution;
        CurrentTrack = track; Status = PlaybackStatus.Loading; Error = null; Position = Duration = 0;
        Changed?.Invoke();
        try
        {
            await _module.InvokeVoidAsync("stop", version);
            var resource = await provider.ResolveAsync(track, resolution.Token);
            resolution.Token.ThrowIfCancellationRequested();
            if (version != _version) return;
            var url = await proxy.PublishAsync(resource, version, resolution.Token);
            if (version != _version) return;
            await _module.InvokeVoidAsync("load", url, version);
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
        if (_module is null) return;
        if (Status == PlaybackStatus.Error && CurrentTrack is { } track) { await PlayAsync(track); return; }
        await _module.InvokeVoidAsync(Status == PlaybackStatus.Playing ? "pause" : "resume");
    }
    public async Task StopAsync()
    {
        _version++; _resolution?.Cancel(); proxy.Clear();
        if (_module is not null) await _module.InvokeVoidAsync("stop", _version);
        CurrentTrack = null; Status = PlaybackStatus.Idle; Position = Duration = 0; Error = null; Changed?.Invoke();
    }
    public async Task SeekAsync(double seconds)
    {
        if (_module is not null && Duration > 0) await _module.InvokeVoidAsync("seek", Math.Clamp(seconds, 0, Duration));
    }
    public async Task SetVolumeAsync(double value)
    {
        Volume = Math.Clamp(value, 0, 1);
        if (_module is not null) await _module.InvokeVoidAsync("volume", Volume);
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
        if (type == "error") Error = "O áudio está indisponível ou foi bloqueado. Tente outra faixa.";
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
