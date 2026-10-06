using System.Net;
using Voxen.Services;
internal static class SoundCloudWebChecks
{
    public static async Task RunAsync()
    {
        using var provider = new SoundCloudWebService(new HttpClient(new Fixture()));
        var tracks = await provider.SearchAsync("test");
        if (tracks.Count != 1 || tracks[0].Id != "123" || tracks[0].Duration?.TotalSeconds != 12) throw new Exception("Public search must filter previews and blocked tracks");
        var resource = await provider.ResolveAsync(tracks[0], default);
        await using var stream = await resource.OpenAsync(default);
        var playlist = await new StreamReader(stream).ReadToEndAsync();
        if (!resource.IsHls || !playlist.Contains("https://cdn.example/segment.aac")) throw new Exception("Public manifest normalization failed");
        Console.WriteLine("PASS SoundCloud public adapter discovers client, refreshes on rejection, filters previews and normalizes HLS");
    }
    private sealed class Fixture : HttpMessageHandler
    {
        private bool _rejected;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            if (uri.Host == "api-v2.soundcloud.com" && !_rejected)
            { _rejected = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)); }
            var track = "{\"id\":123,\"title\":\"Fixture\",\"duration\":12000,\"user\":{\"username\":\"Artist\"},\"permalink_url\":\"https://soundcloud.com/u/t\",\"policy\":\"ALLOW\",\"streamable\":true,\"track_authorization\":\"public-auth\",\"media\":{\"transcodings\":[{\"url\":\"https://api-v2.soundcloud.com/media/test\",\"snipped\":false,\"format\":{\"protocol\":\"hls\",\"mime_type\":\"audio/mpeg\"}}]}}";
            var body = uri.Host switch
            {
                "soundcloud.com" => "<script src=\"https://a-v2.sndcdn.com/app.js\"></script><script src=\"https://untrusted.example/skip.js\"></script>",
                "a-v2.sndcdn.com" => "client_id:\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"",
                "cdn.example" => "#EXTM3U\n#EXTINF:12,\nsegment.aac\n#EXT-X-ENDLIST",
                "api-v2.soundcloud.com" => uri.AbsolutePath switch
                {
                    "/search/tracks" => "{\"collection\":[" + track + "," + track.Replace("\"ALLOW\"", "\"BLOCK\"") + "," + track.Replace("\"snipped\":false", "\"snipped\":true") + "]}",
                    "/tracks/123" => track,
                    "/media/test" => "{\"url\":\"https://cdn.example/playlist.m3u8\"}",
                    _ => throw new Exception("Unexpected API path")
                },
                _ => throw new Exception("Unexpected public adapter request")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(body) });
        }
    }
}
