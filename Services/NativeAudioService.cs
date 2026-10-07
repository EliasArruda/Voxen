using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using SDL;
namespace Voxen.Services;

public sealed record AudioSignal(long Version, string Type, double Position, double Duration);
// FFmpeg decodes a bounded PCM pipe; SDL's bundled native runtime sends it to the OS audio device.
public sealed class NativeAudioService : IAsyncDisposable
{
    private const int BytesPerSecond = 48000 * 2 * 2;
    private readonly SemaphoreSlim _commands = new(1);
    private readonly Channel<AudioSignal> _events = Channel.CreateUnbounded<AudioSignal>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _delivery, _pump;
    private int _timePending;
    private AudioSignal? _latestTime;
    private CancellationTokenSource? _decode;
    private Process? _process;
    private nint _stream;
    private string _url = "", _ffmpeg = "";
    private long _version, _written;
    private double _start, _duration, _volume = .7;
    private bool _paused;
    private AudioTone _tone = new();
    public void SetTone(AudioTone tone) => Volatile.Write(ref _tone, tone.Safe());
    public bool Ready { get; private set; }
    public async Task InitializeAsync(Func<AudioSignal, Task> receive)
    {
        if (Ready) return;
        _ffmpeg = FindFfmpeg();
        OpenDevice(); Ready = true;
        _delivery = Task.Run(async () => {
            await foreach (var signal in _events.Reader.ReadAllAsync(_lifetime.Token))
            {
                var delivery = signal;
                if (signal.Type == "time") { delivery = Volatile.Read(ref _latestTime) ?? signal; Interlocked.Exchange(ref _timePending, 0); }
                try
                {
                    var receiving = receive(delivery);
                    // An ended handler can fetch a recommendation; it must not stall newer native state events.
                    if (delivery.Type == "ended") _ = ObserveAsync(receiving);
                    else await receiving;
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
                catch (Exception) { System.Diagnostics.Trace.WriteLine("Voxen: audio event dispatch failed."); }
            }
        });
        await Task.CompletedTask;
    }
    public static string FindFfmpeg()
    {
        var configured = Environment.GetEnvironmentVariable("VOXEN_FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return Path.GetFullPath(configured);
        var executable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var bundled = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", executable);
        if (File.Exists(bundled)) return bundled;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(directory, executable);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("FFmpeg não encontrado. Use o pacote portátil do Voxen ou configure VOXEN_FFMPEG_PATH.");
    }
    public async Task LoadAsync(string url, long version, double duration)
    {
        await _commands.WaitAsync();
        try { await StopCoreAsync(); _url = url; _version = version; _duration = duration; StartDecoder(0, false); }
        finally { _commands.Release(); }
    }
    public async Task StopAsync(long version)
    {
        await _commands.WaitAsync();
        try { await StopCoreAsync(); _version = version; _url = ""; _written = 0; _start = 0; }
        finally { _commands.Release(); }
    }
    public async Task SeekAsync(double seconds)
    {
        await _commands.WaitAsync();
        try { if (_url.Length == 0) return; var paused = _paused; await StopCoreAsync(); StartDecoder(seconds, paused); Emit("seeked"); }
        finally { _commands.Release(); }
    }
    public async Task PauseAsync()
    {
        await _commands.WaitAsync();
        try { if (_url.Length > 0) { PauseDevice(); _paused = true; Emit("pause"); } }
        finally { _commands.Release(); }
    }
    public async Task ResumeAsync()
    {
        await _commands.WaitAsync();
        try { if (_url.Length > 0) { ResumeDevice(); _paused = false; Emit("playing"); } }
        finally { _commands.Release(); }
    }
    public Task VolumeAsync(double value) { _volume = Math.Clamp(value, 0, 1); SetGain(); return Task.CompletedTask; }
    private void StartDecoder(double seconds, bool paused)
    {
        _start = seconds; _written = 0; _paused = paused; ClearDevice(); SetGain(); PauseDevice();
        var start = new ProcessStartInfo(_ffmpeg) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in new[] { "-hide_banner", "-loglevel", "error", "-nostdin", "-threads", "1", "-analyzeduration", "0", "-probesize", "32768", "-rw_timeout", "10000000", "-ss", seconds.ToString(CultureInfo.InvariantCulture), "-i", _url, "-vn", "-f", "s16le", "-acodec", "pcm_s16le", "-ac", "2", "-ar", "48000", "pipe:1" }) start.ArgumentList.Add(argument);
        var process = new Process { StartInfo = start };
        if (!process.Start()) throw new IOException("Não foi possível iniciar FFmpeg.");
        _process = process; _decode = new CancellationTokenSource();
        _pump = PumpAsync(process, _decode.Token);
    }
    private async Task PumpAsync(Process process, CancellationToken token)
    {
        var buffer = new byte[16384]; var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        // Drain stderr continuously so a failed decoder cannot block behind a full pipe. Never log signed URLs.
        var errors = process.StandardError.ReadToEndAsync(token);
        var tone = Volatile.Read(ref _tone); var processor = new AudioToneProcessor(tone);
        var carry = 0;
        var started = false; var lastReport = Stopwatch.StartNew();
        try
        {
            while (true)
            {
                while (Queued() > BytesPerSecond || (_paused && started))
                { if (lastReport.ElapsedMilliseconds >= 250) { Emit("time"); lastReport.Restart(); } await Task.Delay(30, token); }
                var read = await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(carry), token).AsTask().WaitAsync(TimeSpan.FromSeconds(25), token);
                if (read == 0) break;
                read += carry;
                var complete = read - read % 4;
                if (complete == 0) { carry = read; continue; }
                var updatedTone = Volatile.Read(ref _tone);
                if (tone != updatedTone) { tone = updatedTone; processor = new AudioToneProcessor(tone); }
                processor.Process(buffer.AsSpan(0, complete));
                Put(pinned.AddrOfPinnedObject(), complete); _written += complete;
                carry = read - complete;
                if (carry > 0) Buffer.BlockCopy(buffer, complete, buffer, 0, carry);
                if (!started) { started = true; if (!_paused) { ResumeDevice(); Emit("playing"); } else Emit("pause"); }
                if (lastReport.ElapsedMilliseconds >= 250) { Emit("time"); lastReport.Restart(); }
            }
            await process.WaitForExitAsync(token); await errors;
            if (process.ExitCode != 0 || !started) { Emit("error"); return; }
            _duration = _start + (double)_written / BytesPerSecond;
            FlushDevice();
            while (Queued() > 0) { if (lastReport.ElapsedMilliseconds >= 250) { Emit("time"); lastReport.Restart(); } await Task.Delay(30, token); }
            token.ThrowIfCancellationRequested(); Emit("ended");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) { if (!token.IsCancellationRequested) Emit("error"); }
        finally { pinned.Free(); if (!process.HasExited) process.Kill(entireProcessTree: true); try { await errors; } catch (OperationCanceledException) { } }
    }
    private void Emit(string type)
    {
        var signal = new AudioSignal(_version, type, Math.Max(_start, _start + (double)(_written - Queued()) / BytesPerSecond), _duration);
        if (type == "time")
        {
            Volatile.Write(ref _latestTime, signal);
            if (Interlocked.Exchange(ref _timePending, 1) != 0) return;
        }
        _events.Writer.TryWrite(signal);
    }
    private static async Task ObserveAsync(Task task)
    { try { await task; } catch (Exception) { Trace.WriteLine("Voxen: ended handler failed."); } }
    private async Task StopCoreAsync()
    {
        _decode?.Cancel(); PauseDevice();
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        if (_pump is not null) await _pump;
        _decode?.Dispose(); _decode = null; _pump = null;
        _process?.Dispose(); _process = null; ClearDevice();
    }
    private unsafe void OpenDevice()
    {
        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO)) throw new IOException(SDL3.SDL_GetError());
        var spec = new SDL_AudioSpec { format = SDL3.SDL_AUDIO_S16, channels = 2, freq = 48000 };
        _stream = (nint)SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, null, 0);
        if (_stream == 0) { SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO); throw new IOException("Não foi possível abrir o dispositivo de áudio."); }
    }
    private unsafe int Queued() => _stream == 0 ? 0 : Math.Max(0, SDL3.SDL_GetAudioStreamQueued((SDL_AudioStream*)_stream));
    private unsafe void Put(nint data, int length) { if (!SDL3.SDL_PutAudioStreamData((SDL_AudioStream*)_stream, data, length)) throw new IOException("Falha na saída de áudio."); }
    private unsafe void PauseDevice() { if (_stream != 0) SDL3.SDL_PauseAudioStreamDevice((SDL_AudioStream*)_stream); }
    private unsafe void ResumeDevice() { if (_stream != 0) SDL3.SDL_ResumeAudioStreamDevice((SDL_AudioStream*)_stream); }
    private unsafe void ClearDevice() { if (_stream != 0) SDL3.SDL_ClearAudioStream((SDL_AudioStream*)_stream); }
    private unsafe void FlushDevice() { if (_stream != 0) SDL3.SDL_FlushAudioStream((SDL_AudioStream*)_stream); }
    private unsafe void SetGain() { if (_stream != 0) SDL3.SDL_SetAudioStreamGain((SDL_AudioStream*)_stream, (float)_volume); }
    private unsafe void CloseDevice() { if (_stream != 0) SDL3.SDL_DestroyAudioStream((SDL_AudioStream*)_stream); _stream = 0; SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO); }
    public async ValueTask DisposeAsync()
    {
        Ready = false; await StopAsync(++_version); _events.Writer.TryComplete(); _lifetime.Cancel();
        if (_delivery is not null) try { await _delivery; } catch (OperationCanceledException) { }
        CloseDevice(); _lifetime.Dispose(); _commands.Dispose();
    }
}
