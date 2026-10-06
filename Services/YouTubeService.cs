using Voxen.Models;
using YoutubeExplode;

namespace Voxen.Services;

public sealed class YouTubeService(YoutubeClient client) : ITrackSearchProvider
{
    public const int ResultLimit = 20;

    public async Task<IReadOnlyList<Track>> SearchAsync(
        string query, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length < 2)
            return Array.Empty<Track>();

        var tracks = new List<Track>(ResultLimit);
        await foreach (var video in client.Search.GetVideosAsync(query, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Search metadata already contains everything needed: no per-video requests.
            var thumbnail = video.Thumbnails
                .OrderBy(t => Math.Abs(t.Resolution.Width - 320))
                .FirstOrDefault()?.Url ?? string.Empty;
            tracks.Add(new Track(
                video.Id.Value, video.Title, video.Author.ChannelTitle,
                thumbnail, video.Duration, TrackSource.YouTube, video.Url));
            if (tracks.Count == ResultLimit)
                break;
        }
        return tracks;
    }
}
