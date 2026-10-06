using Voxen.Models;
namespace Voxen.Services;
public sealed record QueueEntry(Guid Key, Track Track);
public sealed class QueueService
{
    private readonly List<QueueEntry> _entries = [];
    public IReadOnlyList<QueueEntry> Entries => _entries.AsReadOnly();
    public Guid? CurrentKey { get; private set; }
    public QueueEntry? Current => _entries.Find(entry => entry.Key == CurrentKey);
    public int CurrentIndex => _entries.FindIndex(entry => entry.Key == CurrentKey);
    public event Action? Changed;
    public QueueEntry Add(Track track)
    {
        if (_entries.Count >= 200) throw new InvalidOperationException("A fila comporta até 200 faixas. Remova algumas para continuar.");
        var entry = new QueueEntry(Guid.NewGuid(), track);
        _entries.Add(entry); Changed?.Invoke(); return entry;
    }
    public bool Select(Guid key)
    {
        if (!_entries.Any(entry => entry.Key == key)) return false;
        CurrentKey = key; Changed?.Invoke(); return true;
    }
    public QueueEntry? Next => _entries.ElementAtOrDefault(CurrentIndex + 1);
    public QueueEntry? Previous => _entries.ElementAtOrDefault(CurrentIndex - 1);
    public void Remove(Guid key)
    {
        _entries.RemoveAll(entry => entry.Key == key);
        if (CurrentKey == key) CurrentKey = null;
        Changed?.Invoke();
    }
    public void Clear() { _entries.Clear(); CurrentKey = null; Changed?.Invoke(); }
}
