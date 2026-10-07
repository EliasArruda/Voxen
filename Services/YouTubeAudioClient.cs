using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using YoutubeExplode;

namespace Voxen.Services;

/// <summary>Retains public audio formats before YoutubeExplode validates every media candidate.</summary>
public static class YouTubeAudioClient
{
    public static YoutubeClient Create(HttpMessageHandler? handler = null) => new(new HttpClient(new AudioMetadataHandler(handler ?? new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    })));

    public static string AudioMetadata(string json)
    {
        var root = JsonNode.Parse(json);
        if (root?["streamingData"] is not JsonObject streaming || streaming["adaptiveFormats"] is not JsonArray formats)
            return json;
        var audio = formats.Where(format => format?["mimeType"]?.GetValue<string>().StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true).ToArray();
        // Preserve live/unavailable responses and upstream fallback when no audio format exists.
        if (audio.Length == 0) return json;
        streaming["adaptiveFormats"] = new JsonArray(audio.Select(format => format!.DeepClone()).ToArray());
        streaming.Remove("formats");
        streaming.Remove("dashManifestUrl");
        return root!.ToJsonString();
    }

    private sealed class AudioMetadataHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = await base.SendAsync(request, token);
            if (!response.IsSuccessStatusCode || request.RequestUri?.AbsolutePath != "/youtubei/v1/player") return response;
            try
            {
                var original = await response.Content.ReadAsStringAsync(token);
                var filtered = AudioMetadata(original);
                if (filtered != original)
                {
                    var old = response.Content;
                    response.Content = new StringContent(filtered, Encoding.UTF8, "application/json");
                    old.Dispose();
                }
                return response;
            }
            catch { response.Dispose(); throw; }
        }
    }
}
