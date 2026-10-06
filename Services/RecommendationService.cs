using System.Text.RegularExpressions;
using Voxen.Models;
namespace Voxen.Services;

public sealed record RecommendedTrack(Track Track, string Reason);

/// <summary>Local artist/title affinity, recency exclusions and artist diversity. No opaque external profile.</summary>
public sealed class RecommendationService(ITrackSearchProvider provider, LibraryService? library = null)
{
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
        { "official", "video", "audio", "music", "lyrics", "lyric", "visualizer", "hd", "hq", "the", "feat", "ft", "remastered", "live", "vevo", "topic" };
    public async Task<Track?> FindNextAsync(Track current, IReadOnlyList<QueueEntry> queue, CancellationToken token)
        => (await DiscoverAsync(current, queue, 8, token)).FirstOrDefault()?.Track;
    public async Task<IReadOnlyList<RecommendedTrack>> DiscoverAsync(Track? current, IReadOnlyList<QueueEntry> queue, int limit, CancellationToken token)
    {
        var history = library?.History ?? [];
        var favorites = library?.Favorites ?? [];
        var weighted = history.Select(entry => (Track: entry.Track, Weight: Math.Min(entry.Plays, 10) + 4 * Math.Exp(-(DateTimeOffset.UtcNow - entry.LastPlayed).TotalDays / 14)))
            .Concat(favorites.Select(track => (Track: track, Weight: 8d))).OrderByDescending(entry => entry.Weight).Select(entry => entry.Track);
        var seeds = (current is null ? weighted : new[] { current }.Concat(weighted))
            .DistinctBy(track => Artist(track)).Take(3).ToArray();
        var excluded = queue.Select(entry => LibraryService.Key(entry.Track)).Concat(history.Take(20).Select(entry => LibraryService.Key(entry.Track))).ToHashSet();
        if (current is not null) excluded.Add(LibraryService.Key(current));
        var known = queue.Select(entry => entry.Track).Concat(history.Take(20).Select(entry => entry.Track)).ToArray();
        var candidates = new List<Track>();
        var successful = false;
        var requests = seeds.Length == 0 ? new[] { (Query: "indie alternative soul music", Source: TrackSource.YouTube) }
            : seeds.Select(track => (Query: $"{Artist(track)} music", Source: track.Source)).Distinct().ToArray();
        foreach (var request in requests)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var search = provider is MusicProviders sources ? sources.Search(request.Source) : provider;
                candidates.AddRange(await search.SearchAsync(request.Query, token)); successful = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (HttpRequestException) { }
            catch (InvalidOperationException) { }
        }
        if (!successful) throw new HttpRequestException("Não foi possível buscar novas recomendações.");
        var scored = candidates.DistinctBy(LibraryService.Key)
            .Where(track => !excluded.Contains(LibraryService.Key(track)) && !known.Any(item => SameRecording(item, track)))
            .Where(track => track.Duration is null || track.Duration.Value.TotalSeconds is >= 45 and <= 1200)
            .Select(track => (Track: track, Score: Score(track, seeds, history, favorites), Reason: Reason(track, seeds)))
            .OrderByDescending(item => item.Score).ThenBy(item => item.Track.Title, StringComparer.OrdinalIgnoreCase);
        var selected = new List<RecommendedTrack>(); var perArtist = new Dictionary<string, int>();
        foreach (var item in scored)
        {
            var artist = Artist(item.Track).ToLowerInvariant();
            if (perArtist.GetValueOrDefault(artist) >= 2 || selected.Any(entry => SameRecording(entry.Track, item.Track))) continue;
            selected.Add(new(item.Track, item.Reason)); perArtist[artist] = perArtist.GetValueOrDefault(artist) + 1;
            if (selected.Count >= Math.Clamp(limit, 1, 20)) break;
        }
        return selected;
    }
    public static string Artist(Track track)
    {
        // YouTube uploaders often differ from the performer. A conventional "Artist - Song" title is a better seed.
        var split = Regex.Split(track.Title, "\\s+[–—-]\\s+");
        var name = split.Length > 1 && Words(split[1]).Count > 0 && split[0].Length is > 1 and < 65 ? split[0] : track.Artist;
        name = Regex.Replace(name, "(?:VEVO| - Topic)$", "", RegexOptions.IgnoreCase).Trim();
        return name.Length > 0 ? name : track.Artist;
    }
    private static HashSet<string> Words(string value) => Regex.Matches(value.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}")
        .Select(match => match.Value).Where(word => !Noise.Contains(word)).ToHashSet();
    private static bool SameRecording(Track a, Track b)
    {
        if (a.Source == b.Source && a.Id == b.Id) return true;
        var left = Words(a.Title); var right = Words(b.Title);
        return Artist(a).Equals(Artist(b), StringComparison.OrdinalIgnoreCase) && left.Count > 0 && right.Count > 0
            && left.Intersect(right).Count() / (double)Math.Max(left.Count, right.Count) >= .8;
    }
    private static double Score(Track track, Track[] seeds, IReadOnlyList<ListeningEntry> history, IReadOnlyList<Track> favorites)
    {
        var artist = Artist(track); var words = Words(track.Title);
        var affinity = seeds.Select((seed, index) => (artist.Equals(Artist(seed), StringComparison.OrdinalIgnoreCase) ? 6d : 0)
            + words.Intersect(Words(seed.Title + " " + seed.Artist)).Count() * 1.5 + (seed.Source == track.Source ? .5 : 0) - index * .2).DefaultIfEmpty(0).Max();
        affinity += favorites.Count(t => Artist(t).Equals(artist, StringComparison.OrdinalIgnoreCase)) * 2;
        affinity += Math.Min(5, history.Where(t => Artist(t.Track).Equals(artist, StringComparison.OrdinalIgnoreCase)).Sum(t => t.Plays) * .3);
        if (history.Any(t => LibraryService.Key(t.Track) == LibraryService.Key(track))) affinity -= 4;
        return affinity;
    }
    private static string Reason(Track track, Track[] seeds)
    {
        var seed = seeds.FirstOrDefault(item => Artist(item).Equals(Artist(track), StringComparison.OrdinalIgnoreCase)) ?? seeds.FirstOrDefault();
        return seed is null ? "Uma descoberta para começar" : $"A partir de {Artist(seed)}";
    }
}
