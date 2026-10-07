using System.Net;
using System.Net.Http.Headers;
using Microsoft.JSInterop;
using Voxen.Models;
using Voxen.Services;
internal static class PlaybackChecks
{
    static Track Track(string id) => new(id, id, "Artist", "", null, TrackSource.YouTube, "https://youtube.com");
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); Console.WriteLine($"PASS {message}"); }
    public static async Task RunAsync()
    {
        var queue = new QueueService();
        var first = queue.Add(Track("same")); var second = queue.Add(Track("same"));
        Check(first.Key != second.Key, "Duplicate tracks have distinct queue identities");
        queue.Select(first.Key);
        Check(queue.Next == second && queue.Previous is null, "Queue advances in insertion order");
        queue.Remove(second.Key); Check(queue.Current == first && queue.Next is null, "Removing another entry preserves selection");
        queue.Clear(); Check(queue.Current is null && queue.Entries.Count == 0, "Clearing queue resets selection");
        for (var i = 0; i < 200; i++) queue.Add(Track(i.ToString()));
        var bounded = false; try { queue.Add(Track("overflow")); } catch (InvalidOperationException) { bounded = true; }
        Check(bounded && queue.Entries.Count == 200, "Queue has a bounded memory footprint");
        queue.Clear();
        await using var proxy = new AudioProxy();
        var bytes = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(bytes)));
        var url = await proxy.PublishAsync(resource, 7, default);
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(10, 19);
        using var response = await http.SendAsync(request);
        Check(response.StatusCode == HttpStatusCode.PartialContent && (await response.Content.ReadAsByteArrayAsync()).SequenceEqual(bytes[10..20]), "Audio proxy serves exact HTTP byte ranges for seek");
        var wrongUrl = new Uri(url).GetLeftPart(UriPartial.Authority) + "/wrong/audio/7";
        Check((await http.GetAsync(wrongUrl)).StatusCode == HttpStatusCode.NotFound, "Audio endpoint rejects unknown session token");
        proxy.Clear(); Check((await http.GetAsync(url)).StatusCode == HttpStatusCode.NotFound, "Cleared playback URL expires");
        var audio = new TestAudio(resource);
        await using var player = new PlayerService(audio, proxy);
        var js = new TestJS(); await player.InitializeAsync(js);
        using var coordinator = new PlaybackCoordinator(queue, player, new RecommendationService(new RecommendationProvider()));
        await coordinator.PlayTrackAsync(Track("one"));
        coordinator.Add(Track("two"));
        await player.OnAudioEvent(js.Module.Version, "playing", 2, 12);
        Check(player.Status == PlaybackStatus.Playing && player.Position == 2 && player.Duration == 12, "Native audio events update player state");
        var oldVersion = js.Module.Version;
        await player.OnAudioEvent(oldVersion, "ended", 12, 12);
        Check(player.CurrentTrack?.Id == "two" && queue.Current?.Track.Id == "two", "Track end automatically advances queue");
        await player.OnAudioEvent(oldVersion, "error", 0, 0);
        Check(player.Status == PlaybackStatus.Loading && player.CurrentTrack?.Id == "two", "Late browser events cannot overwrite next track");
        await coordinator.ToggleAutoplay();
        await player.OnAudioEvent(js.Module.Version, "ended", 12, 12);
        Check(player.CurrentTrack?.Id == "recommended", "Autoplay appends an unseen recommendation at queue end");
        await player.SetVolumeAsync(2); Check(player.Volume == 1, "Player clamps volume to valid native range");
        await coordinator.ClearAsync(); Check(player.CurrentTrack is null && queue.Entries.Count == 0, "Clear queue also stops audio");
        audio.Fail = true; await player.PlayAsync(Track("broken"));
        Check(player.Status == PlaybackStatus.Error && player.Error is not null, "Audio resolution failure exposes retry state");
        audio.Fail = false; await player.ToggleAsync(); Check(player.Status == PlaybackStatus.Loading && player.Error is null, "Retry clears prior audio error");
        await player.OnAudioEvent(js.Module.Version, "error", 0, 12);
        await player.OnAudioEvent(js.Module.Version, "playing", 1, 12);
        Check(player.Status == PlaybackStatus.Playing && player.Error is null, "Late successful playback clears an earlier audio error");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        js.Module.StopGate = gate;
        var stopping = player.StopAsync();
        js.Module.StopGate = null;
        await player.PlayAsync(Track("after-stop"));
        gate.SetResult(); await stopping;
        Check(player.CurrentTrack?.Id == "after-stop" && player.Status == PlaybackStatus.Loading, "Late stop cannot clear the newest playback state");
        var release = new TaskCompletionSource<AudioResource>(TaskCreationOptions.RunContinuationsAsynchronously);
        audio.Pending = release;
        var pending = player.PlayAsync(Track("stale"));
        audio.Pending = null; await player.PlayAsync(Track("new"));
        release.SetResult(resource); await pending;
        Check(player.CurrentTrack?.Id == "new" && player.Status == PlaybackStatus.Loading, "Cancelled audio resolution cannot replace newest track");
    }
    public static async Task RecommendationRaceChecksAsync()
    {
        foreach (var stop in new[] { true, false })
        {
            var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[10])));
            await using var proxy = new AudioProxy();
            await using var player = new PlayerService(new TestAudio(resource), proxy);
            var js = new TestJS(); await player.InitializeAsync(js);
            var queue = new QueueService();
            var delayed = new DelayedRecommendation();
            using var coordinator = new PlaybackCoordinator(queue, player, new RecommendationService(delayed));
            await coordinator.PlayTrackAsync(Track("original")); await coordinator.ToggleAutoplay();
            await player.OnAudioEvent(js.Module.Version, "playing", 90, 100);
            await delayed.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Check(player.Status == PlaybackStatus.Playing, "Autoplay prefetch starts before last track ends");
            var ending = player.OnAudioEvent(js.Module.Version, "ended", 100, 100);
            if (stop) await coordinator.StopAsync(); else coordinator.Add(Track("manual"));
            delayed.Release.SetResult([Track("recommended")]); await ending;
            if (stop) Check(player.CurrentTrack is null && queue.Entries.Count == 1, "Stop cancels pending recommendation and prevents autoplay restart");
            else Check(player.CurrentTrack?.Id == "manual" && queue.Entries.Count == 2, "Manual queue addition wins over late recommendation");
        }
    }
    public static async Task DisablePrefetchedAutoplayAsync()
    {
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        await using var proxy = new AudioProxy();
        await using var player = new PlayerService(new TestAudio(resource), proxy);
        var js = new TestJS(); await player.InitializeAsync(js);
        var queue = new QueueService();
        using var coordinator = new PlaybackCoordinator(queue, player, new RecommendationService(new RecommendationProvider()));
        await coordinator.PlayTrackAsync(Track("one")); await coordinator.ToggleAutoplay();
        await player.OnAudioEvent(js.Module.Version, "playing", 90, 100);
        Check(coordinator.Notice == "Próxima recomendação pronta." && queue.Entries.Count == 1, "Prefetch keeps recommendation outside manual queue until needed");
        await coordinator.ToggleAutoplay();
        await player.OnAudioEvent(js.Module.Version, "ended", 100, 100);
        Check(player.CurrentTrack?.Id == "one" && player.Status == PlaybackStatus.Ended && queue.Entries.Count == 1,
            "Disabling autoplay discards completed prefetch before track end");
    }
    public static async Task ContinueAfterEndAsync()
    {
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        await using var proxy = new AudioProxy();
        await using var player = new PlayerService(new TestAudio(resource), proxy);
        var js = new TestJS(); await player.InitializeAsync(js);
        var queue = new QueueService();
        using var coordinator = new PlaybackCoordinator(queue, player, new RecommendationService(new RecommendationProvider()));
        await coordinator.PlayTrackAsync(Track("one"));
        await player.OnAudioEvent(js.Module.Version, "ended", 12, 12);
        await coordinator.ToggleAutoplay();
        Check(player.CurrentTrack?.Id == "recommended" || player.CurrentTrack?.Id == "two", "Enabling discovery after queue ended starts a recommendation immediately");
    }
    public static async Task StartupDeadlineAsync()
    {
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream()));
        await using var proxy = new AudioProxy();
        var clock = new ManualTime();
        await using var player = new PlayerService(new TestAudio(resource), proxy, clock: clock);
        var js = new TestJS(); await player.InitializeAsync(js);
        await player.PlayAsync(Track("stalled"));
        var old = js.Module.Version;
        clock.Advance(TimeSpan.FromSeconds(21));
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (player.Status != PlaybackStatus.Error || js.Module.Stops < 2) { if (DateTime.UtcNow > deadline) throw new Exception("Startup deadline did not expire"); await Task.Delay(10); }
        Check(player.Status == PlaybackStatus.Error && js.Module.Stops >= 2, "End-to-end startup deadline stops backend after metadata resolves but audio stalls");
        await player.OnAudioEvent(old, "playing", 1, 10);
        Check(player.Status == PlaybackStatus.Error, "Late audio after startup expiry cannot revive failed load");
        await player.ToggleAsync();
        await player.OnAudioEvent(js.Module.Version, "playing", 1, 10);
        clock.Advance(TimeSpan.FromSeconds(21));
        await Task.Yield();
        Check(player.Status == PlaybackStatus.Playing, "Successful playback cancels startup deadline");
    }
    public static async Task ManualNextAndMuteAsync()
    {
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        await using var proxy = new AudioProxy();
        await using var player = new PlayerService(new TestAudio(resource), proxy);
        var js = new TestJS(); await player.InitializeAsync(js);
        var queue = new QueueService();
        using var coordinator = new PlaybackCoordinator(queue, player, new RecommendationService(new RecommendationProvider()));
        await coordinator.PlayTrackAsync(Track("original"));
        await player.OnAudioEvent(js.Module.Version, "playing", 1, 100);
        await coordinator.NextAsync();
        Check(!coordinator.Autoplay && player.CurrentTrack?.Id != "original" && queue.Entries.Count == 2,
            "Manual next finds another track before ending without enabling autoplay");
        coordinator.Add(Track("queued"));
        await coordinator.NextAsync();
        Check(player.CurrentTrack?.Id == "queued", "Manual next prefers the existing queue");
        await player.SetVolumeAsync(.43); var revision = player.VolumeRevision; await player.ToggleMuteAsync();
        await player.SetVolumeFromControlAsync(.2, revision);
        Check(player.Volume == 0, "Mute sets volume to zero and rejects pending older slider changes");
        await player.ToggleMuteAsync();
        Check(Math.Abs(player.Volume - .43) < .001, "Unmute restores the last nonzero volume");
        foreach (var stop in new[] { true, false })
        {
            await using var otherProxy = new AudioProxy();
            await using var other = new PlayerService(new TestAudio(resource), otherProxy);
            await other.InitializeAsync(new TestJS());
            var pending = new DelayedRecommendation();
            var otherQueue = new QueueService();
            using var next = new PlaybackCoordinator(otherQueue, other, new RecommendationService(pending));
            await next.PlayTrackAsync(Track("old"));
            var requesting = next.NextAsync(); await pending.Started.Task;
            if (stop) await next.StopAsync(); else await next.PlayTrackAsync(Track("manual"));
            Check(!next.FindingNext, "Manual choice immediately clears pending-next state");
            pending.Release.SetResult([Track("late")]); await requesting;
            Check(other.CurrentTrack?.Id == (stop ? null : "manual"), "Late manual-next result cannot override stop or selection");
        }
    }
    public static async Task EndDuringManualNextAsync()
    {
        var resource = new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        await using var proxy = new AudioProxy();
        await using var player = new PlayerService(new TestAudio(resource), proxy);
        var js = new TestJS(); await player.InitializeAsync(js);
        var provider = new DelayedRecommendation(); var queue = new QueueService();
        using var playback = new PlaybackCoordinator(queue, player, new RecommendationService(provider));
        await playback.PlayTrackAsync(Track("original"));
        var requesting = playback.NextAsync(); await provider.Started.Task;
        playback.Add(Track("queued"));
        await player.OnAudioEvent(js.Module.Version, "ended", 100, 100);
        provider.Release.SetException(new HttpRequestException("Fixture failure"));
        await requesting;
        Check(player.CurrentTrack?.Id == "queued", "Queued track advances after ending during a failed manual-next request");
    }
    sealed class DelayedRecommendation : ITrackSearchProvider
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IReadOnlyList<Track>> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
        { Started.TrySetResult(); return Release.Task; }
    }
    sealed class TestAudio(AudioResource resource) : IAudioSourceProvider
    {
        public bool Fail; public TaskCompletionSource<AudioResource>? Pending;
        public Task<AudioResource> ResolveAsync(Track track, CancellationToken token) => Pending?.Task ?? (Fail ? Task.FromException<AudioResource>(new IOException()) : Task.FromResult(resource));
    }
    sealed class RecommendationProvider : ITrackSearchProvider
    {
        public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Track>>([Track("one"), Track("two"), Track("recommended")]);
    }
    sealed class TestJS : IJSRuntime
    {
        public TestModule Module { get; } = new();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult((TValue)(object)Module);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }
    sealed class TestModule : IJSObjectReference
    {
        public long Version; public int Stops;
        public TaskCompletionSource? StopGate;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "stop") Stops++;
            if (identifier == "stop" && StopGate is { } gate) return new ValueTask<TValue>(WaitForStop<TValue>(gate.Task));
            if (identifier == "load") Version = (long)args![1]!;
            return ValueTask.FromResult(default(TValue)!);
        }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<TValue>(identifier, args);
        private static async Task<T> WaitForStop<T>(Task gate) { await gate; return default!; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
