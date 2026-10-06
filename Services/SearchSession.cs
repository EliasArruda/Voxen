using Microsoft.Extensions.Logging;
using Voxen.Models;

namespace Voxen.Services;

public enum SearchStatus { Idle, Waiting, Loading, Ready, Error }

/// <summary>Page-owned search state. Cancels pending work and rejects stale responses.</summary>
public sealed class SearchSession(ITrackSearchProvider provider, ILogger<SearchSession> logger) : IDisposable
{
    public const int DebounceMilliseconds = 350;
    private CancellationTokenSource? _pending;
    private long _version;
    private bool _disposed;

    public event Action? Changed;
    public string Query { get; private set; } = string.Empty;
    public IReadOnlyList<Track> Results { get; private set; } = Array.Empty<Track>();
    public SearchStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task UpdateAsync(string input, bool retry = false)
    {
        if (_disposed) return;
        var query = input.Trim();
        if (!retry && query == Query) return;

        _pending?.Cancel();
        var version = ++_version;
        Query = query;
        ErrorMessage = null;
        Results = Array.Empty<Track>();
        if (query.Length < 2)
        {
            _pending = null;
            Status = SearchStatus.Idle;
            Changed?.Invoke();
            return;
        }

        using var request = new CancellationTokenSource();
        _pending = request;
        Status = SearchStatus.Waiting;
        Changed?.Invoke();
        try
        {
            await Task.Delay(DebounceMilliseconds, request.Token);
            if (!IsCurrent(version)) return;
            Status = SearchStatus.Loading;
            Changed?.Invoke();
            request.CancelAfter(TimeSpan.FromSeconds(20));
            var results = await provider.SearchAsync(query, request.Token);
            if (!IsCurrent(version)) return;
            request.Token.ThrowIfCancellationRequested();
            Results = results;
            Status = SearchStatus.Ready;
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            if (!IsCurrent(version)) return;
            Status = SearchStatus.Error;
            ErrorMessage = "A pesquisa demorou demais. Tente novamente.";
        }
        catch (Exception exception)
        {
            if (!IsCurrent(version)) return;
            logger.LogWarning(exception, "YouTube search failed");
            Status = SearchStatus.Error;
            ErrorMessage = "Não foi possível pesquisar no YouTube. Verifique sua conexão e tente novamente.";
        }
        finally
        {
            if (ReferenceEquals(_pending, request)) _pending = null;
        }
        if (IsCurrent(version)) Changed?.Invoke();
    }

    private bool IsCurrent(long version) => !_disposed && version == _version;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ++_version;
        _pending?.Cancel();
        _pending = null;
        Results = Array.Empty<Track>();
        Query = string.Empty;
        ErrorMessage = null;
        Changed = null;
    }
}
