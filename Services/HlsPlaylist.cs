using System.Text.RegularExpressions;
namespace Voxen.Services;

internal static class HlsPlaylist
{
    public static string Normalize(string playlist, Uri origin)
    {
        if (!playlist.TrimStart().StartsWith("#EXTM3U", StringComparison.Ordinal))
            throw new InvalidOperationException("A fonte não forneceu um manifesto de áudio válido.");
        return string.Join('\n', playlist.Split('\n').Select(line => line.StartsWith('#')
            ? Regex.Replace(line, "URI=\"([^\"]+)\"", match => $"URI=\"{new Uri(origin, match.Groups[1].Value)}\"")
            : string.IsNullOrWhiteSpace(line) ? line : new Uri(origin, line.Trim()).ToString()));
    }
}
