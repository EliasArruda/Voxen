using System.Net;
using System.Text;
using Voxen.Services;
internal static class SoundCloudChecks
{
    public static async Task RunAsync()
    {
        var handler = new FixtureHandler();
        using var provider = new SoundCloudService(new HttpClient(handler), "test-id", "test-secret");
        var tracks = await provider.SearchAsync("artist");
        if (tracks.Single().Id != "soundcloud:tracks:123" || tracks[0].Artist != "Uploader" || tracks[0].Duration?.TotalSeconds != 12) throw new Exception("SoundCloud metadata mapping failed");
        Console.WriteLine("PASS SoundCloud maps official URN metadata and millisecond duration");
        var resource = await provider.ResolveAsync(tracks[0], default);
        await using var stream = await resource.OpenAsync(default);
        var playlist = await new StreamReader(stream).ReadToEndAsync();
        if (!resource.IsHls || !playlist.Contains("https://api.soundcloud.com/audio/segment.aac") || playlist.Contains("test-secret")) throw new Exception("SoundCloud HLS normalization failed");
        if (handler.Tokens != 1) throw new Exception("SoundCloud token was not reused");
        Console.WriteLine("PASS SoundCloud caches OAuth token and resolves HLS without exposing credentials");
        using var disabled = new SoundCloudService(new HttpClient(handler), null, null);
        if (disabled.IsConfigured) throw new Exception("SoundCloud should be disabled");
        var denied = false; try { await disabled.SearchAsync("query"); } catch (InvalidOperationException) { denied = true; }
        if (!denied || handler.Tokens != 1) throw new Exception("Unconfigured SoundCloud made a request");
        Console.WriteLine("PASS Missing SoundCloud credentials disable provider without requests");
    }
    sealed class FixtureHandler : HttpMessageHandler
    {
        public int Tokens;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            string body;
            if (request.RequestUri!.AbsolutePath == "/oauth/token")
            {
                Tokens++;
                var basic = request.Headers.Authorization;
                if (basic?.Scheme != "Basic" || Encoding.UTF8.GetString(Convert.FromBase64String(basic.Parameter!)) != "test-id:test-secret") throw new Exception("Invalid token authentication");
                body = "{\"access_token\":\"test-token\",\"refresh_token\":\"test-refresh\",\"expires_in\":3600}";
            }
            else
            {
                if (request.Headers.Authorization?.ToString() != "OAuth test-token") throw new Exception("Missing OAuth request header");
                body = request.RequestUri.AbsolutePath switch
                {
                    "/tracks" => "{\"collection\":[{\"urn\":\"soundcloud:tracks:123\",\"title\":\"Fixture\",\"user\":{\"username\":\"Uploader\"},\"duration\":12000,\"permalink_url\":\"https://soundcloud.com/u/t\"}]}",
                    var path when path.EndsWith("/streams") => "{\"hls_aac_160_url\":\"https://api.soundcloud.com/audio/playlist.m3u8\"}",
                    "/audio/playlist.m3u8" => "#EXTM3U\n#EXTINF:12,\nsegment.aac\n#EXT-X-ENDLIST\n",
                    _ => throw new Exception("Unexpected fixture URL")
                };
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(body) });
        }
    }
}
