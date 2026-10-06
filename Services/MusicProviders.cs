using Voxen.Models;
namespace Voxen.Services;
public sealed class MusicProviders(YouTubeService youtube, SoundCloudService soundCloud) : ITrackSearchProvider, IAudioSourceProvider
{
    public bool SoundCloudAvailable => soundCloud.IsConfigured;
    public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => youtube.SearchAsync(query, cancellationToken);
    public ITrackSearchProvider Search(TrackSource source) => source == TrackSource.SoundCloud ? soundCloud : youtube;
    public Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken) => track.Source == TrackSource.SoundCloud
        ? soundCloud.ResolveAsync(track, cancellationToken) : youtube.ResolveAsync(track, cancellationToken);
}
