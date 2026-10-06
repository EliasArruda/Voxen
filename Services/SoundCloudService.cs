using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Voxen.Models;
namespace Voxen.Services;
public sealed class SoundCloudService : ITrackSearchProvider, IAudioSourceProvider, IDisposable
{
    private readonly HttpClient _http;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly SemaphoreSlim _authentication = new(1);
    private string? _accessToken, _refreshToken;
    private DateTimeOffset _expires;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);
    public SoundCloudService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(20) },
        Environment.GetEnvironmentVariable("VOXEN_SOUNDCLOUD_CLIENT_ID"), Environment.GetEnvironmentVariable("VOXEN_SOUNDCLOUD_CLIENT_SECRET")) { }
    public SoundCloudService(HttpClient http, string? clientId, string? clientSecret) { _http = http; _clientId = clientId; _clientSecret = clientSecret; }
    private async Task<string> TokenAsync(CancellationToken token)
    {
        if (!IsConfigured) throw new InvalidOperationException("Configure as credenciais do SoundCloud e reinicie o Voxen.");
        await _authentication.WaitAsync(token);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expires) return _accessToken;
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://secure.soundcloud.com/oauth/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
            request.Content = new FormUrlEncodedContent(_refreshToken is null
                ? new Dictionary<string,string> { ["grant_type"] = "client_credentials" }
                : new Dictionary<string,string> { ["grant_type"] = "refresh_token", ["refresh_token"] = _refreshToken });
            using var response = await _http.SendAsync(request, token);
            if (!response.IsSuccessStatusCode) { _refreshToken = null; response.EnsureSuccessStatusCode(); }
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
            _accessToken = json.RootElement.GetProperty("access_token").GetString()!;
            _refreshToken = Text(json.RootElement, "refresh_token");
            _expires = DateTimeOffset.UtcNow.AddSeconds(json.RootElement.GetProperty("expires_in").GetDouble() - 60);
            return _accessToken;
        }
        finally { _authentication.Release(); }
    }
    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", await TokenAsync(token));
        using var response = await _http.SendAsync(request, token);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) _accessToken = null;
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
    }
    public async Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (query.Trim().Length < 2) return [];
        using var json = await GetJsonAsync($"https://api.soundcloud.com/tracks?q={Uri.EscapeDataString(query.Trim())}&access=playable&limit=20&linked_partitioning=true", cancellationToken);
        var collection = json.RootElement.ValueKind == JsonValueKind.Array ? json.RootElement : json.RootElement.GetProperty("collection");
        return collection.EnumerateArray().Take(20).Select(item => new Track(
            Text(item, "urn") ?? item.GetProperty("id").ToString(), Text(item, "title") ?? "Sem título",
            Text(item, "metadata_artist") ?? Text(item.GetProperty("user"), "username") ?? "SoundCloud",
            Text(item, "artwork_url") ?? Text(item.GetProperty("user"), "avatar_url") ?? "",
            item.TryGetProperty("duration", out var duration) && duration.TryGetDouble(out var value) ? TimeSpan.FromMilliseconds(value) : null,
            TrackSource.SoundCloud, Text(item, "permalink_url") ?? "https://soundcloud.com")).ToArray();
    }
    public async Task<AudioResource> ResolveAsync(Track track, CancellationToken cancellationToken)
    {
        using var json = await GetJsonAsync($"https://api.soundcloud.com/tracks/{Uri.EscapeDataString(track.Id)}/streams", cancellationToken);
        var url = Text(json.RootElement, "hls_aac_160_url") ?? Text(json.RootElement, "hls_mp3_128_url")
            ?? throw new InvalidOperationException("Esta faixa não está disponível para streaming completo.");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", await TokenAsync(cancellationToken));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > 262144) throw new InvalidOperationException("Manifesto de áudio excede o limite.");
            buffer.Write(chunk, 0, read);
        }
        var playlist = Encoding.UTF8.GetString(buffer.ToArray());
        if (playlist.Length > 262144 || !playlist.StartsWith("#EXTM3U")) throw new InvalidOperationException("Manifesto de áudio não suportado.");
        var baseUri = response.RequestMessage!.RequestUri!;
        // Segment URLs remain signed CDN URLs; OAuth secrets never enter the WebView.
        var normalized = string.Join("\n", playlist.Split('\n').Select(line =>
            line.StartsWith('#') ? Regex.Replace(line, "URI=\"([^\"]+)\"", match => $"URI=\"{new Uri(baseUri, match.Groups[1].Value)}\"")
            : string.IsNullOrWhiteSpace(line) ? line : new Uri(baseUri, line.Trim()).ToString()));
        var bytes = Encoding.UTF8.GetBytes(normalized);
        return new AudioResource("application/vnd.apple.mpegurl", _ => Task.FromResult<Stream>(new MemoryStream(bytes)), IsHls: true);
    }
    private static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
    public void Dispose() { _http.Dispose(); _authentication.Dispose(); }
}
