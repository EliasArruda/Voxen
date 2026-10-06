using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Voxen.Models;

namespace Voxen.Services;

/// <summary>Public website adapter. No user credentials; internal endpoints can change independently of the official API.</summary>
public sealed class SoundCloudWebService : ITrackSearchProvider, IAudioSourceProvider, IDisposable
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _clientGate = new(1);
    private string? _clientId;
    private DateTimeOffset _clientExpires;
    public SoundCloudWebService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(20) }) { }
    public SoundCloudWebService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 Voxen/0.2");
    }
    private async Task<string> ReadAsync(string url, int limit, CancellationToken token)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream();
        var buffer = new byte[8192]; int read;
        while ((read = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (output.Length + read > limit) throw new InvalidOperationException("Resposta do SoundCloud excedeu o limite. Tente novamente.");
            output.Write(buffer, 0, read);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }
    private async Task<string> ClientAsync(CancellationToken token)
    {
        await _clientGate.WaitAsync(token);
        try
        {
            if (_clientId is not null && DateTimeOffset.UtcNow < _clientExpires) return _clientId;
            var page = await ReadAsync("https://soundcloud.com/", 2_000_000, token);
            var scripts = Regex.Matches(page, "<script[^>]+src=\"([^\"]+)\"")
                .Select(match => match.Groups[1].Value).Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.Scheme == "https" && uri.Host == "a-v2.sndcdn.com").Reverse().Take(12);
            foreach (var url in scripts)
            {
                var script = await ReadAsync(url, 5_000_000, token);
                var match = Regex.Match(script, "client_id\\s*:\\s*\"([0-9a-zA-Z]{32})\"");
                if (!match.Success) continue;
                _clientExpires = DateTimeOffset.UtcNow.AddHours(6);
                return _clientId = match.Groups[1].Value;
            }
            throw new InvalidOperationException("A integração pública do SoundCloud mudou. Tente atualizar o Voxen ou use o YouTube.");
        }
        finally { _clientGate.Release(); }
    }
    private async Task<JsonDocument> ApiAsync(string url, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            var client = await ClientAsync(token);
            try { return JsonDocument.Parse(await ReadAsync(url + (url.Contains('?') ? "&" : "?") + "client_id=" + client, 2_000_000, token)); }
            catch (HttpRequestException exception) when (attempt == 0 && exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            { _clientExpires = DateTimeOffset.MinValue; }
        }
    }
    public async Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (query.Trim().Length < 2) return [];
        using var json = await ApiAsync($"https://api-v2.soundcloud.com/search/tracks?q={Uri.EscapeDataString(query.Trim())}&limit=20", cancellationToken);
        return json.RootElement.GetProperty("collection").EnumerateArray().Where(Playable).Take(20).Select(Map).ToArray();
    }
    private static bool Playable(JsonElement item) => Text(item, "policy") == "ALLOW"
        && item.TryGetProperty("streamable", out var streamable) && streamable.ValueKind == JsonValueKind.True
        && item.TryGetProperty("media", out var media) && media.TryGetProperty("transcodings", out var transcodings)
        && transcodings.EnumerateArray().Any(Compatible);
    private static bool Compatible(JsonElement item) => (!item.TryGetProperty("snipped", out var snipped) || snipped.ValueKind != JsonValueKind.True)
        && item.TryGetProperty("format", out var format) && Text(format, "protocol") is "hls" or "progressive";
    private static Track Map(JsonElement item) => new(item.GetProperty("id").ToString(), Text(item, "title") ?? "Sem título",
        Text(item.GetProperty("user"), "username") ?? "SoundCloud", Text(item, "artwork_url") ?? Text(item.GetProperty("user"), "avatar_url") ?? "",
        item.TryGetProperty("duration", out var duration) && duration.TryGetDouble(out var value) ? TimeSpan.FromMilliseconds(value) : null,
        TrackSource.SoundCloud, Text(item, "permalink_url") ?? "https://soundcloud.com");
    public async Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken)
    {
        var id = track.Id.Split(':').Last();
        if (!long.TryParse(id, out _)) throw new InvalidOperationException("Identificador do SoundCloud inválido.");
        using var json = await ApiAsync($"https://api-v2.soundcloud.com/tracks/{id}", cancellationToken);
        if (!Playable(json.RootElement)) throw new InvalidOperationException("Esta faixa não permite reprodução pública completa.");
        var transcoding = json.RootElement.GetProperty("media").GetProperty("transcodings").EnumerateArray()
            .Where(Compatible).OrderBy(item => Text(item.GetProperty("format"), "protocol") != "progressive").First();
        var endpoint = Text(transcoding, "url")!;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var api) || api.Scheme != "https" || api.Host != "api-v2.soundcloud.com")
            throw new InvalidOperationException("Formato de áudio do SoundCloud não suportado.");
        var authorization = Text(json.RootElement, "track_authorization");
        using var resolved = await ApiAsync(endpoint + (endpoint.Contains('?') ? "&" : "?") + "track_authorization=" + Uri.EscapeDataString(authorization ?? ""), cancellationToken);
        var url = Text(resolved.RootElement, "url")!;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var streamUri) || streamUri.Scheme != "https") throw new InvalidOperationException("URL de áudio inválida.");
        if (Text(transcoding.GetProperty("format"), "protocol") == "progressive")
            return new AudioResource(Text(transcoding.GetProperty("format"), "mime_type") ?? "audio/mpeg", token => _http.GetStreamAsync(url, token));
        var playlist = await ReadAsync(url, 262_144, cancellationToken);
        if (!playlist.StartsWith("#EXTM3U")) throw new InvalidOperationException("Manifesto do SoundCloud não suportado.");
        var normalized = string.Join('\n', playlist.Split('\n').Select(line => line.StartsWith('#')
            ? Regex.Replace(line, "URI=\"([^\"]+)\"", match => $"URI=\"{new Uri(streamUri, match.Groups[1].Value)}\"")
            : string.IsNullOrWhiteSpace(line) ? line : new Uri(streamUri, line.Trim()).ToString()));
        var bytes = Encoding.UTF8.GetBytes(normalized);
        return new AudioResource("application/vnd.apple.mpegurl", _ => Task.FromResult<Stream>(new MemoryStream(bytes)), IsHls: true);
    }
    private static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    public void Dispose() { _http.Dispose(); _clientGate.Dispose(); }
}
