using Voxen.Models;
namespace Voxen.Services;
public sealed class PlaybackCoordinator : IDisposable
{
    private readonly QueueService _queue;
    private readonly PlayerService _player;
    private readonly RecommendationService _recommendations;
    private CancellationTokenSource? _recommendation;
    public bool Autoplay { get; private set; }
    public string? Notice { get; private set; }
    public event Action? Changed;
    public PlaybackCoordinator(QueueService queue, PlayerService player, RecommendationService recommendations)
    { _queue = queue; _player = player; _recommendations = recommendations; player.Ended += AdvanceAsync; }
    private void CancelRecommendation() { _recommendation?.Cancel(); Notice = null; }
    public Task PlayTrackAsync(Track track)
    {
        CancelRecommendation();
        try { return PlayEntryAsync(_queue.Add(track).Key); }
        catch (InvalidOperationException exception) { Notice = exception.Message; Changed?.Invoke(); return Task.CompletedTask; }
    }
    public Task PlayEntryAsync(Guid key)
    {
        CancelRecommendation();
        return _queue.Select(key) && _queue.Current is { } entry ? _player.PlayAsync(entry.Track) : Task.CompletedTask;
    }
    public void Add(Track track)
    {
        try { _queue.Add(track); Notice = "Faixa adicionada à fila."; }
        catch (InvalidOperationException exception) { Notice = exception.Message; }
        Changed?.Invoke();
    }
    public Task NextAsync() { CancelRecommendation(); return _queue.Next is { } entry ? PlayEntryAsync(entry.Key) : Task.CompletedTask; }
    public Task PreviousAsync() { CancelRecommendation(); return _queue.Previous is { } entry ? PlayEntryAsync(entry.Key) : Task.CompletedTask; }
    public async Task RemoveAsync(Guid key)
    {
        CancelRecommendation();
        if (_queue.CurrentKey == key) await _player.StopAsync();
        _queue.Remove(key);
    }
    public async Task ClearAsync() { CancelRecommendation(); await _player.StopAsync(); _queue.Clear(); }
    public void ToggleAutoplay() { Autoplay = !Autoplay; if (!Autoplay) CancelRecommendation(); Changed?.Invoke(); }
    private async Task AdvanceAsync()
    {
        if (_queue.Next is { } next) { await PlayEntryAsync(next.Key); return; }
        if (!Autoplay || _player.CurrentTrack is not { } track || _queue.Entries.Count >= 200) return;
        var request = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _recommendation = request; Notice = "Encontrando o próximo som…"; Changed?.Invoke();
        try
        {
            var recommendation = await _recommendations.FindNextAsync(track, _queue.Entries, request.Token);
            request.Token.ThrowIfCancellationRequested();
            _recommendation = null;
            if (recommendation is not null) await PlayTrackAsync(recommendation);
            else { Notice = "Nenhuma nova recomendação disponível."; Changed?.Invoke(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { Notice = "Não foi possível buscar recomendações. Sua fila continua disponível."; Changed?.Invoke(); }
        finally { if (_recommendation == request) _recommendation = null; request.Dispose(); }
    }
    public void Dispose() { CancelRecommendation(); _player.Ended -= AdvanceAsync; }
}
