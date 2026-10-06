using System.Text;
using Microsoft.JSInterop;
using Voxen.Models;
using Voxen.Services;

internal static class NativeRetryChecks
{
    public static async Task RunAsync()
    {
        using var wav = new MemoryStream();
        using (var writer = new BinaryWriter(wav, Encoding.UTF8, true))
        {
            const int size = 192000 * 3;
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(size + 36);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)2); writer.Write(48000); writer.Write(192000);
            writer.Write((short)4); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(size); writer.Write(new byte[size]);
        }
        var track = new Track("retry", "Retry", "Fixture", "", TimeSpan.FromSeconds(3), TrackSource.SoundCloud, "https://soundcloud.com/test");
        foreach (var recover in new[] { true, false })
        {
            var source = new Source(wav.ToArray(), recover);
            using var preparation = new AudioPreparationService(source);
            await using var proxy = new AudioProxy();
            await using var native = new NativeAudioService();
            await using var player = new PlayerService(preparation, proxy, native);
            await player.InitializeAsync(new NoBrowser());
            await player.PlayAsync(track);
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (player.Status == PlaybackStatus.Loading)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Startup recovery did not finish");
                await Task.Delay(25);
            }
            if (source.Calls != 2 || player.Status != (recover ? PlaybackStatus.Playing : PlaybackStatus.Error))
                throw new Exception("Startup must refresh metadata once and never retry forever");
            await player.StopAsync();
        }
        foreach (var expire in new[] { true, false })
        {
            var clock = new ManualTime();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var source = new Source(wav.ToArray(), true) { PendingRetry = gate.Task, FirstPrepared = () => clock.Advance(TimeSpan.FromSeconds(5)) };
            using var preparation = new AudioPreparationService(source);
            await using var proxy = new AudioProxy();
            await using var native = new NativeAudioService();
            await using var player = new PlayerService(preparation, proxy, native, clock: clock);
            await player.InitializeAsync(new NoBrowser());
            await player.PlayAsync(track);
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (source.Calls < 2)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Retry did not resolve fresh metadata");
                await Task.Delay(25);
            }
            if (expire)
            {
                clock.Advance(TimeSpan.FromSeconds(16));
                while (player.Status == PlaybackStatus.Loading)
                {
                    if (DateTime.UtcNow > deadline) throw new TimeoutException("Retry extended original startup deadline");
                    await Task.Delay(25);
                }
                if (player.Status != PlaybackStatus.Error) throw new Exception("Retry must share original20-second deadline");
            }
            else await player.StopAsync();
            gate.SetResult();
            await Task.Delay(150);
            if (source.Calls != 2 || player.Status != (expire ? PlaybackStatus.Error : PlaybackStatus.Idle))
                throw new Exception("Late retry cannot restart playback after timeout or stop");
        }
        Console.WriteLine("PASS Native startup refreshes stale metadata once; second failure stops retrying");
        Console.WriteLine("PASS Retry preserves original startup deadline; timeout and stop reject late metadata");
    }
    private sealed class Source(byte[] bytes, bool recover) : IAudioSourceProvider
    {
        public int Calls;
        public Task? PendingRetry;
        public Action? FirstPrepared;
        public async Task<AudioResource> ResolveAsync(Track track, CancellationToken token)
        {
            var call = Interlocked.Increment(ref Calls);
            if (call == 1) FirstPrepared?.Invoke();
            if (call == 2 && PendingRetry is { } pending) await pending; // Ignore cancellation to exercise stale-result protection.
            return new AudioResource("audio/wav", _ => call == 1 || !recover
                ? Task.FromException<Stream>(new HttpRequestException("Expired test stream"))
                : Task.FromResult<Stream>(new MemoryStream(bytes)));
        }
    }
    private sealed class NoBrowser : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string id, object?[]? args) => throw new Exception("Native audio must not use browser audio");
        public ValueTask<T> InvokeAsync<T>(string id, CancellationToken token, object?[]? args) => InvokeAsync<T>(id, args);
    }
}
