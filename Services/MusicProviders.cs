using Voxen.Models;
namespace Voxen.Services;
public sealed class MusicProviders(YouTubeService youtube, SoundCloudService soundCloud, SoundCloudWebService publicSoundCloud) : IStreamingTrackSearchProvider, IAudioSourceProvider
{
    public bool SoundCloudAvailable => true;
    public bool SoundCloudOfficial => soundCloud.IsConfigured;
    private CombinedSearchProvider Combined => new(youtube, soundCloud.IsConfigured ? soundCloud : publicSoundCloud);
    public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => Combined.SearchAsync(query, cancellationToken);
    public IAsyncEnumerable<SearchSnapshot> SearchSnapshotsAsync(string query, CancellationToken token = default) => Combined.SearchSnapshotsAsync(query, token);
    public ITrackSearchProvider Search(TrackSource? source) => source is null ? this : source == TrackSource.SoundCloud ? soundCloud.IsConfigured ? soundCloud : publicSoundCloud : youtube;
    public Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken) => track.Source == TrackSource.SoundCloud
        ? (soundCloud.IsConfigured ? (IAudioSourceProvider)soundCloud : publicSoundCloud).ResolveAsync(track, cancellationToken) : youtube.ResolveAsync(track, cancellationToken);
}
