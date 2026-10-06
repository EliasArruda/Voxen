using System.Runtime.CompilerServices;
using Voxen.Models;
namespace Voxen.Services;

public sealed record SearchSnapshot(IReadOnlyList<Track> Tracks, string? Notice, bool Complete);
public interface IStreamingTrackSearchProvider : ITrackSearchProvider
{
    IAsyncEnumerable<SearchSnapshot> SearchSnapshotsAsync(string query, CancellationToken token = default);
}

/// <summary>Both sources search concurrently; the first successful source appears without waiting for the other.</summary>
public sealed class CombinedSearchProvider(ITrackSearchProvider youtube, ITrackSearchProvider soundCloud,
    TimeSpan? sourceTimeout = null) : IStreamingTrackSearchProvider
{
    private sealed record Outcome(TrackSource Source, IReadOnlyList<Track> Tracks, bool Failed);
    public async Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Track> result = [];
        await foreach (var snapshot in SearchSnapshotsAsync(query, cancellationToken)) result = snapshot.Tracks;
        return result;
    }
    public async IAsyncEnumerable<SearchSnapshot> SearchSnapshotsAsync(string query,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var pending = new List<Task<Outcome>> {
            Fetch(youtube, TrackSource.YouTube, query, token), Fetch(soundCloud, TrackSource.SoundCloud, query, token) };
        var completed = new List<Outcome>();
        while (pending.Count > 0)
        {
            var task = await Task.WhenAny(pending).WaitAsync(token);
            pending.Remove(task); completed.Add(await task);
            token.ThrowIfCancellationRequested();
            if (pending.Count == 0 && completed.All(item => item.Failed))
                throw new HttpRequestException("As duas fontes estão indisponíveis.");
            var yt = completed.FirstOrDefault(item => item.Source == TrackSource.YouTube)?.Tracks ?? [];
            var sc = completed.FirstOrDefault(item => item.Source == TrackSource.SoundCloud)?.Tracks ?? [];
            var tracks = Enumerable.Range(0, Math.Max(yt.Count, sc.Count))
                .SelectMany(index => new[] { yt.ElementAtOrDefault(index), sc.ElementAtOrDefault(index) })
                .OfType<Track>().DistinctBy(track => (track.Source, track.Id)).Take(40).ToArray();
            var failed = completed.Where(item => item.Failed).Select(item => item.Source.ToString()).ToArray();
            yield return new(tracks, failed.Length == 0 ? null :
                $"{string.Join(" e ", failed)} indisponível no momento. Os resultados da outra fonte continuam disponíveis.", pending.Count == 0);
        }
    }
    private async Task<Outcome> Fetch(ITrackSearchProvider provider, TrackSource source, string query, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(sourceTimeout ?? TimeSpan.FromSeconds(12));
        try { return new(source, (await provider.SearchAsync(query, deadline.Token).WaitAsync(deadline.Token)).Take(20).ToArray(), false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { return new(source, [], true); }
    }
}
