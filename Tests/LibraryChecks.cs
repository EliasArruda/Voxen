using Voxen.Models;
using Voxen.Services;
using Microsoft.JSInterop;
internal static class LibraryChecks
{
    private static Track Track(string id, string artist = "Artist", string? title = null) => new(id, title ?? id, artist, "", TimeSpan.FromMinutes(3), TrackSource.YouTube, "https://youtube.com/watch?v=" + id);
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    public static async Task RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "voxen-checks-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "library.json"); var store = new LibraryService(file);
            var track = Track("saved"); store.ToggleSaved(track); store.ToggleFavorite(track);
            var playlist = store.CreatePlaylist("Road"); store.AddToPlaylist(playlist, track); store.AddToPlaylist(playlist, track);
            store.RenamePlaylist(playlist, "Home"); store.RecordHeard(track); store.RecordHeard(track);
            var reopened = new LibraryService(file);
            Check(reopened.Saved.Count == 1 && reopened.Favorites.Count == 1 && reopened.Playlists.Single().Name == "Home" && reopened.Playlists.Single().Tracks.Count == 1 && reopened.History.Single().Plays == 2, "Library favorites playlists and history persist without duplicate membership");
            reopened.RemoveFromPlaylist(playlist, track); reopened.DeletePlaylist(playlist); reopened.ClearHistory(); reopened.ToggleSaved(track); reopened.ToggleFavorite(track);
            var empty = new LibraryService(file); Check(empty.Saved.Count + empty.Favorites.Count + empty.Playlists.Count + empty.History.Count == 0, "Library removal and history clearing persist");
            File.WriteAllText(file, "{broken"); var damaged = new LibraryService(file); damaged.ToggleSaved(track);
            Check(damaged.Error is not null && File.ReadAllText(file) == "{broken", "Corrupt library is preserved and reported rather than overwritten");
            foreach (var content in new[] { "{\"Version\":2}", "{\"Playlists\":[null]}", "{\"Saved\":[null]}" })
            {
                File.WriteAllText(file, content); var invalid = new LibraryService(file); invalid.ToggleSaved(track);
                Check(invalid.Error is not null && File.ReadAllText(file) == content, "Unsupported or null-entry library preserves file without crashing startup");
            }
            var profile = new LibraryService(Path.Combine(folder, "profile.json"));
            profile.RecordHeard(Track("old", "Preferred Artist", "Jazz blue")); profile.ToggleFavorite(Track("liked", "Preferred Artist", "Jazz blue"));
            var recommendations = await new RecommendationService(new Candidates(), profile).DiscoverAsync(null, [], 8, default);
            Check(recommendations.Count == 3 && recommendations[0].Track.Artist == "Preferred Artist" && recommendations.All(r => r.Track.Id != "old") && recommendations.Count(r => r.Track.Artist == "Preferred Artist") == 2, "Discovery ranks listening affinity excludes recent recordings and limits artist repetition");
            await using var proxy = new AudioProxy(); await using var player = new PlayerService(new Audio(), proxy);
            var module = new JS(); await player.InitializeAsync(module);
            var heard = new LibraryService(Path.Combine(folder, "heard.json")); using var observer = new ListeningHistoryService(player, heard);
            await player.PlayAsync(Track("heard")); await player.OnAudioEvent(module.Module.Version, "playing", 0, 180);
            await player.OnAudioEvent(module.Module.Version, "time", 90, 180);
            Check(heard.History.Count == 0, "Seeking does not count as a qualified listen");
            for (var i = 1; i <= 35; i++)
            {
                await player.SeekAsync(90 + i); await player.OnAudioEvent(module.Module.Version, "seeked", 90 + i, 180);
            }
            Check(heard.History.Count == 0, "Repeated small seeks do not qualify as listening");
            for (var i = 1; i <= 31; i++) await player.OnAudioEvent(module.Module.Version, "time", 125 + i, 180);
            Check(heard.History.Single().Plays == 1, "Continuous playback records exactly one qualified listen per load");
            await player.OnAudioEvent(module.Module.Version, "pause", 122, 180); await player.OnAudioEvent(module.Module.Version, "time", 180, 180);
            Check(heard.History.Single().Plays == 1, "Paused updates never add listening history");
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
    private sealed class Candidates : ITrackSearchProvider
    {
        public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Track>>([
            Track("random", "Other Artist", "Nightfall"), Track("old", "Preferred Artist", "Jazz blue"), Track("new1", "Preferred Artist", "Jazz sunrise"),
            Track("new2", "Preferred Artist", "Jazz twilight"), Track("new3", "Preferred Artist", "Jazz dawn"), Track("long", "Mix Channel") with { Duration = TimeSpan.FromHours(3) }]);
    }
    private sealed class Audio : IAudioSourceProvider
    { public Task<AudioResource> ResolveAsync(Track track, CancellationToken token) => Task.FromResult(new AudioResource("audio/wav", _ => Task.FromResult<Stream>(new MemoryStream(new byte[12])))); }
    private sealed class JS : IJSRuntime
    {
        public Module Module = new();
        public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) => ValueTask.FromResult((T)(object)Module);
        public ValueTask<T> InvokeAsync<T>(string name, CancellationToken token, object?[]? args) => InvokeAsync<T>(name, args);
    }
    private sealed class Module : IJSObjectReference
    {
        public long Version;
        public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) { if (name == "load") Version = (long)args![1]!; return ValueTask.FromResult(default(T)!); }
        public ValueTask<T> InvokeAsync<T>(string name, CancellationToken token, object?[]? args) => InvokeAsync<T>(name, args);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
