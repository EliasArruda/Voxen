namespace Voxen.Services;

/// <summary>Counts actual forward playback, excluding seeks and paused time. One qualified listen per load.</summary>
public sealed class ListeningHistoryService : IDisposable
{
    private readonly PlayerService _player;
    private readonly LibraryService _library;
    private Voxen.Models.Track? _track;
    private PlaybackStatus _status;
    private double _position, _heard;
    private bool _recorded, _seeking;
    public ListeningHistoryService(PlayerService player, LibraryService library)
    { _player = player; _library = library; player.Changed += Observe; }
    private void Observe()
    {
        if (_player.CurrentTrack != _track || (_player.Status == PlaybackStatus.Loading && _status != PlaybackStatus.Loading))
        { _track = _player.CurrentTrack; _heard = 0; _recorded = false; _position = _player.Position; }
        if (!_player.IsSeeking && !_seeking && _track is not null && _status == PlaybackStatus.Playing && _player.Status is PlaybackStatus.Playing or PlaybackStatus.Ended)
        {
            var delta = _player.Position - _position;
            if (delta is > 0 and <= 3) _heard += delta;
            var threshold = _player.Duration > 0 ? Math.Min(30, Math.Max(5, _player.Duration / 2)) : 30;
            if (!_recorded && _heard >= threshold) { _recorded = true; _library.RecordHeard(_track); }
        }
        _position = _player.Position; _status = _player.Status; _seeking = _player.IsSeeking;
    }
    public void Dispose() => _player.Changed -= Observe;
}
