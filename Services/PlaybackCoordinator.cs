using Voxen.Models;
namespace Voxen.Services;
public sealed class PlaybackCoordinator : IDisposable
{
    private readonly QueueService _queue;
    private readonly PlayerService _player;
    private readonly RecommendationService _recommendations;
    private CancellationTokenSource? _recommendation;
    private Task? _prefetch;
    private Guid? _requestedFor;
    private Track? _candidate;
    public bool Autoplay { get; private set; }
    public string? Notice { get; private set; }
    public event Action? Changed;
    public PlaybackCoordinator(QueueService queue, PlayerService player, RecommendationService recommendations)
    { _queue = queue; _player = player; _recommendations = recommendations; player.Ended += AdvanceAsync; player.Changed += PrefetchWhenNeeded; }
    private void CancelRecommendation() { _recommendation?.Cancel(); _prefetch = null; _requestedFor = null; _candidate = null; Notice = null; }
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
    public async Task StopAsync() { CancelRecommendation(); await _player.StopAsync(); Changed?.Invoke(); }
    public async Task ClearAsync() { CancelRecommendation(); await _player.StopAsync(); _queue.Clear(); }
    public async Task ToggleAutoplay()
    {
        Autoplay = !Autoplay;
        if (!Autoplay) { CancelRecommendation(); Notice = "Reprodução automática desativada."; }
        else
        {
            Notice = "Ativado. Novas descobertas continuam depois da sua fila.";
            PrefetchWhenNeeded();
        }
        Changed?.Invoke();
        if (Autoplay && _player.Status == PlaybackStatus.Ended) await AdvanceAsync();
    }
    private void PrefetchWhenNeeded()
    {
        if (!Autoplay || _player.Status != PlaybackStatus.Playing || _player.Duration <= 0
            || _queue.Next is not null
            || _queue.CurrentKey is not { } key || _requestedFor == key || _queue.Entries.Count >= 200) return;
        _requestedFor = key;
        _prefetch = RequestRecommendationAsync();
    }
    private async Task AdvanceAsync()
    {
        if (_queue.Next is { } next) { await PlayEntryAsync(next.Key); return; }
        if (!Autoplay || _player.CurrentTrack is null || _queue.Entries.Count >= 200) return;
        var key = _queue.CurrentKey;
        if (_prefetch is { } pending) await pending;
        else if (_requestedFor != key || key is null) { _requestedFor = key; await RequestRecommendationAsync(); }
        // Manual additions win over any recommendation fetched while the queue was empty.
        if (!Autoplay || _player.Status != PlaybackStatus.Ended || _queue.CurrentKey != key) return;
        if (_queue.Next is { } entry) await PlayEntryAsync(entry.Key);
        else if (_candidate is { } candidate && _queue.Entries.Count < 200) await PlayEntryAsync(_queue.Add(candidate).Key);
    }
    private async Task RequestRecommendationAsync()
    {
        if (_player.CurrentTrack is not { } track) return;
        var key = _queue.CurrentKey;
        var request = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _recommendation = request; Notice = "Encontrando o próximo som…"; Changed?.Invoke();
        try
        {
            var recommendation = await _recommendations.FindNextAsync(track, _queue.Entries, request.Token);
            request.Token.ThrowIfCancellationRequested();
            if (!Autoplay || _queue.CurrentKey != key || _queue.Next is not null) return;
            if (recommendation is not null) { _candidate = recommendation; Notice = "Próxima recomendação pronta."; }
            else Notice = "Nenhuma nova recomendação disponível.";
            Changed?.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!request.IsCancellationRequested) { Notice = "Não foi possível buscar recomendações. Sua fila continua disponível."; Changed?.Invoke(); } }
        finally { if (_recommendation == request) _recommendation = null; request.Dispose(); }
    }
    public void Dispose() { CancelRecommendation(); _player.Ended -= AdvanceAsync; _player.Changed -= PrefetchWhenNeeded; }
}
