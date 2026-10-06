namespace Voxen.Models;

public enum TrackSource
{
    YouTube,
    SoundCloud
}

/// <summary>Provider-independent metadata. Url identifies the track, not its audio stream.</summary>
public sealed record Track(
    string Id,
    string Title,
    string Artist,
    string ThumbnailUrl,
    TimeSpan? Duration,
    TrackSource Source,
    string Url);
