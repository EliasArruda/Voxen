using Voxen.Models;
using Voxen.Services;
internal static class AudioPreparationChecks
{
    private static Track Track(string id = "music") => new(id, id, "Artist", "", null, TrackSource.YouTube, "https://example.com");
    public static async Task RunAsync()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; var opened = 0;
        var resource = new AudioResource("audio/wav", _ => { opened++; return Task.FromResult<Stream>(new MemoryStream()); });
        var provider = new Provider(async (_, token) => { Interlocked.Increment(ref calls); await release.Task.WaitAsync(token); return resource; });
        var clock = new Clock();
        using var prepared = new AudioPreparationService(provider, clock);
        var warm = prepared.PrepareAsync(Track());
        using var canceled = new CancellationTokenSource();
        var abandoned = prepared.ResolveAsync(Track(), canceled.Token); canceled.Cancel();
        try { await abandoned; throw new Exception("Canceled caller completed"); } catch (OperationCanceledException) { }
        var play = prepared.ResolveAsync(Track(), default);
        release.SetResult(); await Task.WhenAll(warm, play);
        if(calls != 1 || opened != 0) throw new Exception("Preparation duplicated resolution or downloaded audio");
        await prepared.ResolveAsync(Track(), default);
        if(calls != 1) throw new Exception("Warm metadata not reused");
        clock.Advance(TimeSpan.FromSeconds(91)); await prepared.ResolveAsync(Track(), default);
        if(calls != 2) throw new Exception("Expired metadata reused");
        prepared.Invalidate(Track()); await prepared.ResolveAsync(Track(), default);
        if(calls != 3) throw new Exception("Invalidated metadata reused");
        var attempts = 0;
        using var flaky = new AudioPreparationService(new Provider((_, _) => ++attempts == 1 ? Task.FromException<AudioResource>(new IOException()) : Task.FromResult(resource)));
        await flaky.PrepareAsync(Track()); await flaky.ResolveAsync(Track(), default);
        if(attempts != 2) throw new Exception("Failed preparation poisoned retry");
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var speculative = 0;
        using var bounded = new AudioPreparationService(new Provider(async (_, token) => { Interlocked.Increment(ref speculative); await blocked.Task.WaitAsync(token); return resource; }));
        var tasks = Enumerable.Range(0, 12).Select(i => bounded.PrepareAsync(Track(i.ToString()))).ToArray();
        await Task.Delay(100); blocked.SetResult(); await Task.WhenAll(tasks);
        if(speculative != AudioPreparationService.Capacity) throw new Exception("Speculative metadata requests exceeded capacity");
        var durableCalls = 0;
        using var durable = new AudioPreparationService(new Provider((_, _) => { durableCalls++; return Task.FromResult(resource); }), preparationTimeout: TimeSpan.FromMilliseconds(50));
        await durable.ResolveAsync(Track(), default); await Task.Delay(120); await durable.ResolveAsync(Track(), default);
        if (durableCalls != 1) throw new Exception("Successful metadata expired with its resolution deadline instead of cache lifetime");
        var promotionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var promoted = new AudioPreparationService(new Provider(async (_, token) => { await promotionGate.Task.WaitAsync(token); return resource; }), preparationTimeout: TimeSpan.FromMilliseconds(300));
        var speculativeWarm = promoted.PrepareAsync(Track());
        await Task.Delay(200);
        var promotedPlay = promoted.ResolveAsync(Track(), default);
        await Task.Delay(200); promotionGate.SetResult();
        await Task.WhenAll(speculativeWarm, promotedPlay);
        Console.WriteLine("PASS Foreground playback renews a nearly expired speculative deadline");
        Console.WriteLine("PASS Prepared metadata shares work, avoids audio downloads, survives caller cancellation, expires, invalidates, retries and bounds speculation");
    }
    private sealed class Provider(Func<Track, CancellationToken, Task<AudioResource>> resolve) : IAudioSourceProvider
    { public Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken) => resolve(track, cancellationToken); }
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }
}
