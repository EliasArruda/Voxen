using Microsoft.Extensions.Logging.Abstractions;
using Voxen.Models;
using Voxen.Services;
using YoutubeExplode;

static Track TrackFor(string query) => new(query, query, "Artist", "", null, TrackSource.YouTube, "https://youtube.com");
static void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine($"PASS {description}");
}
static SearchSession Session(FakeProvider fake) => new(fake, NullLogger<SearchSession>.Instance);

var fake = new FakeProvider();
using (var session = Session(fake))
{
    var first = session.UpdateAsync("link");
    var second = session.UpdateAsync("linkin");
    var third = session.UpdateAsync("linkin park");
    await Task.WhenAll(first, second, third);
    Check(fake.Calls.SequenceEqual(["linkin park"]), "Rapid input sends only the final query");
    Check(session.Results.Single().Id == "linkin park", "Final query updates results");
    await session.UpdateAsync(" linkin park ");
    Check(fake.Calls.Count == 1, "Identical trimmed query avoids duplicate request");
    await session.UpdateAsync("a");
    Check(session.Status == SearchStatus.Idle && session.Results.Count == 0 && fake.Calls.Count == 1, "Short input clears results without network request");
}

var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
CancellationToken oldToken = default;
fake = new FakeProvider(async (query, token) =>
{
    if (query == "old") { oldToken = token; started.SetResult(); await release.Task; }
    return [TrackFor(query)]; // Deliberately ignore cancellation to exercise stale response protection.
});
using (var session = Session(fake))
{
    var old = session.UpdateAsync("old");
    await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
    await session.UpdateAsync("new");
    Check(oldToken.IsCancellationRequested, "New query cancels in-flight request");
    release.SetResult();
    await old;
    Check(session.Results.Single().Id == "new", "Late old response cannot overwrite new results");
}

var attempts = 0;
fake = new FakeProvider((query, token) => ++attempts == 1
    ? Task.FromException<IReadOnlyList<Track>>(new HttpRequestException("test"))
    : Task.FromResult<IReadOnlyList<Track>>([]));
using (var session = Session(fake))
{
    await session.UpdateAsync("query");
    Check(session.Status == SearchStatus.Error && session.ErrorMessage is not null, "Provider failure produces actionable error");
    await session.UpdateAsync("query", retry: true);
    Check(session.Status == SearchStatus.Ready && session.Results.Count == 0, "Retry same query can recover with empty results");
}

fake = new FakeProvider();
var disposedSession = Session(fake);
var pending = disposedSession.UpdateAsync("query");
disposedSession.Dispose();
await pending;
Check(fake.Calls.Count == 0, "Disposal cancels debounce before provider call");

if (args.Contains("--youtube"))
{
    var youtube = new YouTubeService(new YoutubeClient());
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    var tracks = await youtube.SearchAsync("Linkin Park Numb", timeout.Token);
    Check(tracks.Count is > 0 and <= YouTubeService.ResultLimit, "Live YouTube search returns bounded results");
    Check(tracks.All(t => t.Id.Length > 0 && t.Title.Length > 0 && t.Artist.Length > 0 && t.Url.StartsWith("https://")), "Live search maps required metadata");
}
await AudioPreparationChecks.RunAsync();
await CombinedSearchChecks.RunAsync();
await PlaybackChecks.StartupDeadlineAsync();
await PlaybackChecks.RunAsync();
await PlaybackChecks.ManualNextAndMuteAsync();
await PlaybackChecks.EndDuringManualNextAsync();
await SoundCloudChecks.RunAsync();
await SoundCloudWebChecks.RunAsync();
await PlaybackChecks.RecommendationRaceChecksAsync();
await PlaybackChecks.DisablePrefetchedAutoplayAsync();
await PlaybackChecks.ContinueAfterEndAsync();
await LibraryChecks.RunAsync();
if (args.Contains("--native")) { await NativeAudioChecks.RunAsync(); await NativeRetryChecks.RunAsync(); }
if (args.Contains("--soundcloud"))
{
    using var publicSoundCloud = new SoundCloudWebService();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var tracks = await publicSoundCloud.SearchAsync("lofi", timeout.Token);
    Check(tracks.Count is > 0 and <= 20, "Live public SoundCloud search without credentials");
    var resource = await publicSoundCloud.ResolveAsync(tracks[0], timeout.Token);
    await using var audio = await resource.OpenAsync(timeout.Token);
    var bytes = new byte[4096];
    Check(await audio.ReadAsync(bytes, timeout.Token) > 0, "Live public SoundCloud resolves readable audio");
}
Console.WriteLine("All checks passed.");

sealed class FakeProvider(Func<string, CancellationToken, Task<IReadOnlyList<Track>>>? handler = null) : ITrackSearchProvider
{
    public List<string> Calls { get; } = [];
    public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        Calls.Add(query);
        return handler?.Invoke(query, cancellationToken) ?? Task.FromResult<IReadOnlyList<Track>>(
            [new(query, query, "Artist", "", null, TrackSource.YouTube, "https://youtube.com")]);
    }
}
