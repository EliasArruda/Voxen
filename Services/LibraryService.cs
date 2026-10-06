using System.Text.Json;
using Voxen.Models;

namespace Voxen.Services;

public sealed record ListeningEntry(Track Track, int Plays, DateTimeOffset LastPlayed);
public sealed record MusicPlaylist(Guid Id, string Name, List<Track> Tracks);
public sealed class LibraryData
{
    public int Version { get; set; } = 1;
    public List<Track> Saved { get; set; } = [];
    public List<Track> Favorites { get; set; } = [];
    public List<MusicPlaylist> Playlists { get; set; } = [];
    public List<ListeningEntry> History { get; set; } = [];
}

/// <summary>Small metadata-only local store. Writes replace the file atomically; audio and credentials are never stored.</summary>
public sealed class LibraryService
{
    private readonly string _path;
    private LibraryData _data = new();
    public string? Error { get; private set; }
    public event Action? Changed;
    public long Revision { get; private set; }
    public IReadOnlyList<Track> Saved => _data.Saved.AsReadOnly();
    public IReadOnlyList<Track> Favorites => _data.Favorites.AsReadOnly();
    public IReadOnlyList<MusicPlaylist> Playlists => _data.Playlists.Select(p => p with { Tracks = [.. p.Tracks] }).ToArray();
    public IReadOnlyList<ListeningEntry> History => _data.History.AsReadOnly();
    public string StoragePath => _path;
    public LibraryService() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voxen", "library.json")) { }
    public LibraryService(string path)
    {
        _path = path;
        try
        {
            if (!File.Exists(path)) return;
            if (new FileInfo(path).Length > 8_000_000) throw new InvalidDataException("Biblioteca grande demais.");
            var data = JsonSerializer.Deserialize<LibraryData>(File.ReadAllText(path));
            if (data is null || data.Version != 1 || data.Saved is null || data.Favorites is null || data.Playlists is null || data.History is null)
                throw new InvalidDataException("Formato de biblioteca inválido.");
            if (data.Saved.Count > 2000 || data.Favorites.Count > 2000 || data.Playlists.Count > 100 || data.History.Count > 1000
                || data.Playlists.Any(p => p is null || p.Tracks is null || p.Tracks.Count > 500)) throw new InvalidDataException("Limites de biblioteca excedidos.");
            if (data.Saved.Concat(data.Favorites).Any(t => !Valid(t))
                || data.Playlists.Any(p => string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 60 || p.Tracks.Any(t => !Valid(t)))
                || data.History.Any(h => h is null || h.Plays < 1 || !Valid(h.Track))) throw new InvalidDataException("Metadados inválidos.");
            _data = data;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        { Error = "Não foi possível carregar sua biblioteca. O arquivo original foi preservado; verifique o armazenamento antes de salvar."; _loadFailed = true; }
    }
    private static bool Valid(Track? track) => track is not null && !string.IsNullOrWhiteSpace(track.Id) && track.Id.Length <= 200
        && track.Title is { Length: > 0 and <= 2000 } && track.Artist is { Length: <= 500 }
        && track.ThumbnailUrl is { Length: <= 2000 } && (track.ThumbnailUrl.Length == 0 || Https(track.ThumbnailUrl))
        && Https(track.Url) && Enum.IsDefined(track.Source) && (track.Duration is null || track.Duration.Value.TotalSeconds >= 0);
    private static bool Https(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https";
    private bool _loadFailed;
    public static string Key(Track track) => $"{track.Source}:{track.Id}";
    public bool IsSaved(Track track) => _data.Saved.Any(t => Key(t) == Key(track));
    public bool IsFavorite(Track track) => _data.Favorites.Any(t => Key(t) == Key(track));
    private void Save()
    {
        Revision++;
        if (_loadFailed) { Changed?.Invoke(); return; }
        var temporary = _path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var content = JsonSerializer.Serialize(_data);
            if (System.Text.Encoding.UTF8.GetByteCount(content) > 8_000_000) throw new IOException("Metadata store exceeds limit.");
            File.WriteAllText(temporary, content);
            File.Move(temporary, _path, overwrite: true); Error = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { Error = "Alterações estão nesta sessão, mas não foram salvas. Verifique espaço e permissões do armazenamento."; }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { } }
        Changed?.Invoke();
    }
    public void ToggleSaved(Track track)
    {
        if (IsSaved(track)) _data.Saved.RemoveAll(t => Key(t) == Key(track));
        else { if (_data.Saved.Count >= 2000) throw new InvalidOperationException("Sua biblioteca comporta até 2.000 faixas."); _data.Saved.Insert(0, track); }
        Save();
    }
    public void ToggleFavorite(Track track)
    {
        if (IsFavorite(track)) _data.Favorites.RemoveAll(t => Key(t) == Key(track));
        else { if (_data.Favorites.Count >= 2000) throw new InvalidOperationException("Você pode favoritar até 2.000 faixas."); _data.Favorites.Insert(0, track); }
        Save();
    }
    public Guid CreatePlaylist(string name)
    {
        name = ValidateName(name);
        if (_data.Playlists.Count >= 100) throw new InvalidOperationException("Você pode criar até 100 playlists.");
        if (_data.Playlists.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Já existe uma playlist com esse nome.");
        var id = Guid.NewGuid(); _data.Playlists.Add(new(id, name, [])); Save(); return id;
    }
    public void RenamePlaylist(Guid id, string name)
    {
        name = ValidateName(name);
        if (_data.Playlists.Any(p => p.Id != id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Já existe uma playlist com esse nome.");
        var index = _data.Playlists.FindIndex(p => p.Id == id);
        if (index < 0) return;
        _data.Playlists[index] = _data.Playlists[index] with { Name = name }; Save();
    }
    private static string ValidateName(string name)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 60) throw new InvalidOperationException("Use um nome entre 1 e 60 caracteres.");
        return name;
    }
    public void DeletePlaylist(Guid id) { if (_data.Playlists.RemoveAll(p => p.Id == id) > 0) Save(); }
    public void AddToPlaylist(Guid id, Track track)
    {
        var playlist = _data.Playlists.Find(p => p.Id == id) ?? throw new InvalidOperationException("Playlist não encontrada.");
        if (playlist.Tracks.Any(t => Key(t) == Key(track))) return;
        if (playlist.Tracks.Count >= 500) throw new InvalidOperationException("Cada playlist comporta até 500 faixas.");
        playlist.Tracks.Add(track); Save();
    }
    public void RemoveFromPlaylist(Guid id, Track track)
    { var playlist = _data.Playlists.Find(p => p.Id == id); if (playlist?.Tracks.RemoveAll(t => Key(t) == Key(track)) > 0) Save(); }
    public void RecordHeard(Track track)
    {
        var prior = _data.History.Find(entry => Key(entry.Track) == Key(track));
        _data.History.RemoveAll(entry => Key(entry.Track) == Key(track));
        _data.History.Insert(0, new(track, (prior?.Plays ?? 0) + 1, DateTimeOffset.UtcNow));
        if (_data.History.Count > 1000) _data.History.RemoveAt(1000);
        Save();
    }
    public void ClearHistory() { _data.History.Clear(); Save(); }
}
