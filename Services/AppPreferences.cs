using System.Text.Json;
namespace Voxen.Services;

public sealed record AudioTone(double Bass = 0, double Mid = 0, double Treble = 0)
{
    public AudioTone Safe() => new(Clamp(Bass), Clamp(Mid), Clamp(Treble));
    private static double Clamp(double value) => double.IsFinite(value) ? Math.Clamp(value, -12, 12) : 0;
}
public sealed record PreferenceData(string Language = "pt-BR", string Preset = "flat", AudioTone? Tone = null, bool CoverColors = true);
public sealed class AppPreferences : IDisposable
{
    private readonly string _path;
    private readonly object _writeGate = new();
    private CancellationTokenSource? _pendingSave;
    public PreferenceData Data { get; private set; } = new();
    public AudioTone Tone => Data.Tone ?? new();
    public string? Error { get; private set; }
    public event Action? Changed;
    public AppPreferences() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voxen", "settings.json")) { }
    public AppPreferences(string path)
    {
        _path = path;
        try { if (File.Exists(path) && new FileInfo(path).Length < 16_384) Data = Validate(JsonSerializer.Deserialize<PreferenceData>(File.ReadAllText(path)) ?? new()); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { Error = "Não foi possível carregar as configurações."; }
    }
    private static PreferenceData Validate(PreferenceData data) => data with { Language = data.Language is "pt-BR" or "en" or "es" ? data.Language : "pt-BR", Tone = (data.Tone ?? new()).Safe(), Preset = data.Preset is "flat" or "bass" or "voice" or "bright" or "custom" ? data.Preset : "flat" };
    public void Language(string value) => Save(Data with { Language = value });
    public void CoverColors(bool value) => Save(Data with { CoverColors = value });
    public void Equalizer(AudioTone tone) => Save(Data with { Preset = "custom", Tone = tone.Safe() }, defer: true);
    public void Preset(string value) => Save(Data with { Preset = value, Tone = value switch { "bass" => new(8, -2, 1), "voice" => new(-3, 4, 2), "bright" => new(0, 0, 5), _ => new() } });
    private void Save(PreferenceData data, bool defer = false)
    {
        CancellationTokenSource? pending=null;
        lock(_writeGate)
        {
            _pendingSave?.Cancel(); _pendingSave=null;
            Data = Validate(data); Error = null;
            if(defer) { pending=new();_pendingSave=pending; }
            else Persist();
        }
        Changed?.Invoke();
        if(pending is not null)_ = PersistLater(pending);
    }
    private async Task PersistLater(CancellationTokenSource pending)
    {
        try
        {
            await Task.Delay(300,pending.Token);
            lock(_writeGate) { if(pending.IsCancellationRequested)return;Persist();if(_pendingSave==pending)_pendingSave=null; }
            Changed?.Invoke();
        }
        catch(OperationCanceledException) { }
        finally { pending.Dispose(); }
    }
    private void Persist()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(Data)); File.Move(_path + ".tmp", _path, true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Error = "Não foi possível salvar as configurações. As alterações valem nesta sessão."; }
    }
    public void Dispose() { lock(_writeGate) { if(_pendingSave is not null) { _pendingSave.Cancel();_pendingSave=null;Persist(); } } }
}
