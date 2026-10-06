using Voxen.Models;
namespace Voxen.Services;
public sealed record AudioResource(string ContentType, Func<CancellationToken, Task<Stream>> OpenAsync, bool IsHls = false);
public interface IAudioSourceProvider
{
    Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken);
}
