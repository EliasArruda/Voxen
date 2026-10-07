using Voxen.Models;
namespace Voxen.Services;

/// <summary>Short-lived stream metadata, never audio files. Concurrent prepare/play requests share one resolution.</summary>
public sealed class AudioPreparationService(IAudioSourceProvider provider, TimeProvider? clock = null,
    TimeSpan? lifetime = null, TimeSpan? preparationTimeout = null) : IAudioSourceProvider, IAudioSourceInvalidation, IDisposable
{
    private sealed record Entry(Task<AudioResource> Task, DateTimeOffset Expires, CancellationTokenSource Deadline);
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = [];
    private readonly CancellationTokenSource _shutdown = new();
    private readonly TimeSpan _timeout = preparationTimeout ?? TimeSpan.FromSeconds(20);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private bool _disposed;
    public const int Capacity = 8;
    private static string Key(Track track) => $"{track.Source}:{track.Id}";
    public async Task PrepareAsync(Track track)
    {
        Entry? entry = null;
        try
        {
            entry = Get(track);
            if (entry is not null) await entry.Task;
        }
        catch (Exception) { if (entry is not null) Remove(Key(track), entry); } // Preparation cannot change playback state or surface an error.
    }
    public async Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = Get(track, promote: true);
        if (entry is null) return await provider.ResolveAsync(track, cancellationToken);
        try { return await entry.Task.WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { Remove(Key(track), entry); throw; }
    }
    private Entry? Get(Track track, bool promote = false)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var now = _clock.GetUtcNow();
            foreach (var key in _entries.Where(pair => pair.Value.Expires <= now || pair.Value.Task.IsFaulted || pair.Value.Task.IsCanceled || (!pair.Value.Task.IsCompletedSuccessfully && pair.Value.Deadline.IsCancellationRequested)).Select(pair => pair.Key).ToArray())
                RemoveCore(key);
            if (_entries.TryGetValue(Key(track), out var cached))
            {
                if (promote && !cached.Task.IsCompleted) cached.Deadline.CancelAfter(_timeout);
                return cached;
            }
            if (_entries.Count == Capacity)
            {
                var oldest = _entries.Where(pair => pair.Value.Task.IsCompleted).OrderBy(pair => pair.Value.Expires).FirstOrDefault();
                if (oldest.Key is null) return null; // Never evict an in-flight playback request.
                RemoveCore(oldest.Key);
            }
            var deadline = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            deadline.CancelAfter(_timeout);
            var task = Task.Run(async () => await provider.ResolveAsync(track, deadline.Token).WaitAsync(deadline.Token), deadline.Token);
            var entry = new Entry(task, now + (lifetime ?? TimeSpan.FromSeconds(90)), deadline);
            _entries.Add(Key(track), entry);
            return entry;
        }
    }
    public void Invalidate(Track track) { lock (_gate) RemoveCore(Key(track)); if(provider is IAudioSourceInvalidation source) source.Invalidate(track); }
    private void Remove(string key, Entry entry)
    { lock (_gate) { if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry)) RemoveCore(key); } }
    private void RemoveCore(string key)
    {
        if (!_entries.Remove(key, out var entry)) return;
        entry.Deadline.Cancel(); entry.Deadline.Dispose();
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true; _shutdown.Cancel();
            foreach (var key in _entries.Keys.ToArray()) RemoveCore(key);
            _shutdown.Dispose();
        }
    }
}
