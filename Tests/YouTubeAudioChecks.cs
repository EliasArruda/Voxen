using System.Text.Json.Nodes;
using Voxen.Services;

static class YouTubeAudioChecks
{
    public static void Run()
    {
        const string metadata = """
        {"playabilityStatus":{"status":"OK"},"streamingData":{"expiresInSeconds":"3600","adaptiveFormats":[{"itag":140,"mimeType":"audio/mp4; codecs=\"mp4a.40.2\"","signatureCipher":"protected-signature","contentLength":"300"},{"itag":251,"mimeType":"audio/webm","url":"https://example.org/audio"},{"itag":137,"mimeType":"video/mp4","url":"https://example.org/video"}],"formats":[{"mimeType":"video/mp4"}],"dashManifestUrl":"https://example.org/dash","hlsManifestUrl":"https://example.org/live"},"videoDetails":{"title":"Track"}}
        """;
        var filtered=JsonNode.Parse(YouTubeAudioClient.AudioMetadata(metadata))!;
        var formats=(JsonArray)filtered["streamingData"]!["adaptiveFormats"]!;
        Require(formats.Count==2 && formats[0]!["signatureCipher"]!.GetValue<string>()=="protected-signature", "Audio formats retain signed metadata for upstream validation");
        Require(filtered["streamingData"]!["formats"] is null && filtered["streamingData"]!["dashManifestUrl"] is null, "Video candidates do not incur media validation requests");
        Require(filtered["playabilityStatus"]!["status"]!.GetValue<string>()=="OK" && filtered["streamingData"]!["hlsManifestUrl"]!.GetValue<string>()=="https://example.org/live", "Playability and live metadata stay intact");
        foreach(var json in new[]{"{\"playabilityStatus\":{\"status\":\"LOGIN_REQUIRED\"}}", "{\"streamingData\":{\"adaptiveFormats\":[{\"mimeType\":\"video/mp4\"}],\"dashManifestUrl\":\"fallback\"}}", "{\"streamingData\":{\"hlsManifestUrl\":\"live\"}}"})
            Require(YouTubeAudioClient.AudioMetadata(json)==json,"Unavailable/live/non-audio metadata preserves upstream fallback");
    }
    private static void Require(bool condition,string message) { if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message); }
}
