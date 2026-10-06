using System.Collections.Concurrent;
using System.Text;
using Voxen.Services;
internal static class NativeAudioChecks
{
    public static async Task RunAsync()
    {
        using var wav = new MemoryStream();
        using (var writer = new BinaryWriter(wav, Encoding.UTF8, leaveOpen: true))
        {
            const int length = 48000 * 2 * 2 * 3;
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(length + 36); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)2); writer.Write(48000); writer.Write(192000); writer.Write((short)4); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(length); writer.Write(new byte[length]);
        }
        var bytes = wav.ToArray();
        await using var proxy = new AudioProxy();
        var url = await proxy.PublishAsync(new("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(bytes))), 1, default);
        await using var audio = new NativeAudioService();
        var signals = new ConcurrentQueue<AudioSignal>();
        var releaseEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await audio.InitializeAsync(signal => { signals.Enqueue(signal); return signal.Type == "ended" ? releaseEnded.Task : Task.CompletedTask; });
        await audio.LoadAsync(url, 1, 3);
        await Until(() => signals.Any(s => s.Type == "playing") && signals.Last().Position > .3);
        await audio.PauseAsync(); await Until(() => signals.Last().Type == "pause");
        var paused = signals.Last().Position; await Task.Delay(400);
        if (Math.Abs(signals.Last().Position - paused) > .1) throw new Exception("Native pause advanced playback");
        var seeking = audio.SeekAsync(1.5);
        var resuming = audio.ResumeAsync();
        await Task.WhenAll(seeking, resuming);
        await Until(() => signals.Last().Position >= 1.49);
        await audio.VolumeAsync(.2);
        await Until(() => signals.Last().Position > 1.7);
        await audio.SeekAsync(2.8); await Until(() => signals.Last().Type == "ended");
        if (Math.Abs(signals.Last().Duration - 3) > .1) throw new Exception("Native decoded duration incorrect");
        await audio.StopAsync(2);
        await audio.LoadAsync(url, 3, 3);
        await Until(() => signals.Any(signal => signal.Version == 3 && signal.Type == "playing"));
        releaseEnded.SetResult(); await audio.StopAsync(4);
        Console.WriteLine("PASS Slow ended handler cannot block state events for a new native track");
        Console.WriteLine("PASS Native FFmpeg/SDL playback, pause, seek, resume, volume, ended and stop without GStreamer");
    }
    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (!condition()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Native audio check timed out"); await Task.Delay(30); }
    }
}
