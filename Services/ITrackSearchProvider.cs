using Voxen.Models;

namespace Voxen.Services;

/// <summary>Search boundary for YouTube and future music providers.</summary>
public interface ITrackSearchProvider
{
    Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
