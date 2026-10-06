using Voxen.Models;
using Voxen.Services;
internal static class CombinedSearchChecks
{
    private static Track Track(string id, TrackSource source) => new(id, "Music " + id, "Artist", "", null, source, "https://example.com");
    public static async Task RunAsync()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var youtube = new Provider((_, _) => Task.FromResult<IReadOnlyList<Track>>([Track("same", TrackSource.YouTube), Track("same", TrackSource.YouTube)]));
        var sc = new Provider(async (_, token) => { await release.Task.WaitAsync(token); return [Track("same", TrackSource.SoundCloud)]; });
        var combined = new CombinedSearchProvider(youtube, sc);
        await using (var snapshots = combined.SearchSnapshotsAsync("music").GetAsyncEnumerator())
        {
            if (!await snapshots.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2)) || snapshots.Current.Complete || snapshots.Current.Tracks.Count != 1)
                throw new Exception("Fast source waited for slower source or duplicate result remained");
            release.SetResult(); await snapshots.MoveNextAsync();
            if (!snapshots.Current.Complete || snapshots.Current.Tracks.Count != 2 || snapshots.Current.Tracks[0].Source != TrackSource.YouTube || snapshots.Current.Tracks[1].Source != TrackSource.SoundCloud)
                throw new Exception("Combined results lost source identity or order");
        }
        var failed = new Provider((_, _) => Task.FromException<IReadOnlyList<Track>>(new HttpRequestException()));
        var partial = new CombinedSearchProvider(youtube, failed);
        SearchSnapshot? last = null;
        await foreach (var item in partial.SearchSnapshotsAsync("music")) last = item;
        if (last?.Notice is null || last.Tracks.Count != 1) throw new Exception("Partial failure hid valid results");
        var blocked = new Provider(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return []; });
        last = null;
        await foreach (var item in new CombinedSearchProvider(youtube, blocked, TimeSpan.FromMilliseconds(100)).SearchSnapshotsAsync("music")) last = item;
        if (last?.Notice is null || !last.Complete || last.Tracks.Count != 1) throw new Exception("Timeout did not preserve successful source");
        try { await new CombinedSearchProvider(failed, failed).SearchAsync("music"); throw new Exception("Both-source failure was hidden"); }
        catch (HttpRequestException) { }
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        try { await combined.SearchAsync("music", canceled.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS Combined search streams fast results, interleaves source identities, removes duplicates and isolates failures/timeouts/cancellation");
    }
    private sealed class Provider(Func<string, CancellationToken, Task<IReadOnlyList<Track>>> search) : ITrackSearchProvider
    { public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => search(query, cancellationToken); }
}
