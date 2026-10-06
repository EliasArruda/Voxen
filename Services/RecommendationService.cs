using Voxen.Models;
namespace Voxen.Services;
public sealed class RecommendationService(ITrackSearchProvider provider)
{
    public async Task<Track?> FindNextAsync(Track current, IReadOnlyList<QueueEntry> queue, CancellationToken token)
    {
        var candidates = await provider.SearchAsync($"{current.Artist} music", token);
        return candidates.FirstOrDefault(track => !queue.Any(entry => entry.Track.Source == track.Source && entry.Track.Id == track.Id));
    }
}
