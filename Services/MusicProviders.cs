using Voxen.Models;
namespace Voxen.Services;
public sealed class MusicProviders(YouTubeService youtube, SoundCloudService soundCloud, SoundCloudWebService publicSoundCloud) : ITrackSearchProvider, IAudioSourceProvider
{
    public bool SoundCloudAvailable => true;
    public bool SoundCloudOfficial => soundCloud.IsConfigured;
    public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => youtube.SearchAsync(query, cancellationToken);
    public ITrackSearchProvider Search(TrackSource source) => source == TrackSource.SoundCloud ? soundCloud.IsConfigured ? soundCloud : publicSoundCloud : youtube;
    public Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken) => track.Source == TrackSource.SoundCloud
        ? (soundCloud.IsConfigured ? (IAudioSourceProvider)soundCloud : publicSoundCloud).ResolveAsync(track, cancellationToken) : youtube.ResolveAsync(track, cancellationToken);
}
