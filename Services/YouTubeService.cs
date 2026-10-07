using Voxen.Models;
using YoutubeExplode;

namespace Voxen.Services;

public sealed class YouTubeService(YoutubeClient client, YouTubeMusicSearchService music) : ITrackSearchProvider, IAudioSourceProvider
{
    public async Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken)
    {
        if (track.Source != TrackSource.YouTube) throw new NotSupportedException("Fonte de áudio incorreta.");
        try
        {
            var manifest = await client.Videos.Streams.GetManifestAsync(track.Id, cancellationToken);
            var stream = manifest.GetAudioOnlyStreams()
                .OrderByDescending(item => item.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                .ThenByDescending(item => item.Bitrate).FirstOrDefault()
                ?? throw new InvalidOperationException("Esta faixa não oferece áudio público compatível.");
            var contentType = stream.Container == YoutubeExplode.Videos.Streams.Container.Mp4 ? "audio/mp4" : "audio/webm";
            return new AudioResource(contentType, async token => await client.Videos.Streams.GetAsync(stream, token), NativeInputUrl: stream.Url);
        }
        catch (YoutubeExplode.Exceptions.YoutubeExplodeException)
        {
            // A broadcast can start after its search result was cached.
            return await ResolveLiveAsync(track, cancellationToken);
        }
    }
    private async Task<AudioResource> ResolveLiveAsync(Track track, CancellationToken token)
    {
        var url = await client.Videos.Streams.GetHttpLiveStreamUrlAsync(track.Id, token);
        using var http = new HttpClient();
        var playlist = await http.GetStringAsync(url, token);
        // Use an audio rendition instead of probing/downloading every video variant.
        var audio = System.Text.RegularExpressions.Regex.Matches(playlist, @"#EXT-X-MEDIA:[^\r\n]+")
            .Select(match => match.Value).Where(line => line.Contains("TYPE=AUDIO"))
            .Select(line => System.Text.RegularExpressions.Regex.Match(line, "URI=\"([^\"]+)\""))
            .Where(match => match.Success).LastOrDefault();
        if (audio is not null)
        {
            url = new Uri(new Uri(url), audio.Groups[1].Value).ToString();
        }
        return new AudioResource("application/vnd.apple.mpegurl", async cancellation =>
        {
            // Live media sequences move: never cache a playlist snapshot for browser reloads.
            using var request = new HttpClient();
            var current = await request.GetStringAsync(url, cancellation);
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(HlsPlaylist.Normalize(current, new Uri(url))));
        }, IsHls: true, NativeInputUrl: url);
    }
    public const int ResultLimit = 20;

    public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => music.SearchAsync(query, cancellationToken);
}
