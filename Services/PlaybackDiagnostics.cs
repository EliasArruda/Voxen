using Voxen.Models;
namespace Voxen.Services;

/// <summary>Local failure categories only: never URLs, titles, exception messages or credentials.</summary>
internal static class PlaybackDiagnostics
{
    private static readonly object Gate = new();
    public static void Record(TrackSource source, string stage, Exception exception, bool retry)
    {
        var status = exception is HttpRequestException http ? ((int?)http.StatusCode)?.ToString() ?? "none" : "none";
        var line = $"{DateTimeOffset.UtcNow:O} source={source} stage={stage} error={exception.GetType().Name} http={status} retry={retry}";
        Console.Error.WriteLine("Voxen playback: " + line);
        try
        {
            lock (Gate)
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voxen");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "playback-diagnostics.log");
                if (File.Exists(path) && new FileInfo(path).Length > 64 * 1024) File.WriteAllText(path, "");
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
