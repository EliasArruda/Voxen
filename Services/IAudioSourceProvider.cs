using Voxen.Models;
namespace Voxen.Services;
public sealed record AudioResource(string ContentType, Func<CancellationToken, Task<Stream>> OpenAsync, bool IsHls = false, string? NativeInputUrl = null);
public interface IAudioSourceProvider
{
    Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken);
}

public interface IAudioSourceInvalidation
{
    void Invalidate(Track track);
}
