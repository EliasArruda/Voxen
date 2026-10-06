// Virtual one-shot time for startup deadlines; no wall-clock timing assumptions in the regression.
internal sealed class ManualTime : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
    private readonly List<Timer> _timers = [];
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() { lock (_timers) return _now.Ticks; }
    public override DateTimeOffset GetUtcNow() { lock (_timers) return _now; }
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_timers) { var timer = new Timer(this, callback, state); _timers.Add(timer); timer.Change(dueTime, period); return timer; }
    }
    public void Advance(TimeSpan amount)
    {
        Timer[] due;
        lock (_timers)
        {
            _now += amount;
            due = _timers.Where(timer => !timer.Disposed && timer.Due <= _now).ToArray();
            foreach (var timer in due) timer.Due = timer.Period == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : _now + timer.Period;
        }
        foreach (var timer in due) timer.Callback(timer.State);
    }
    private sealed class Timer(ManualTime owner, TimerCallback callback, object? state) : ITimer
    {
        public TimerCallback Callback { get; } = callback;
        public object? State { get; } = state;
        public DateTimeOffset Due; public TimeSpan Period; public bool Disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        { lock(owner._timers) { if(Disposed) return false; Due = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : owner._now + dueTime; Period = period; return true; } }
        public void Dispose() { lock(owner._timers) Disposed = true; }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
