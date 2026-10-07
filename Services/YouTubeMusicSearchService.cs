using System.Net.Http.Json;
using System.Text.Json;
using Voxen.Models;
namespace Voxen.Services;

/// <summary>Public YouTube Music song search. Intentionally never falls back to general video search.</summary>
public sealed class YouTubeMusicSearchService : IDisposable
{
    private readonly HttpClient _http;
    private readonly TimeSpan _timeout;
    public YouTubeMusicSearchService() : this(new HttpClient { Timeout=TimeSpan.FromSeconds(20) }) { }
    public YouTubeMusicSearchService(HttpClient http,TimeSpan? timeout=null) { _http=http;_timeout=timeout??TimeSpan.FromSeconds(20);_http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 Voxen/0.3"); }
    public async Task<IReadOnlyList<Track>> SearchAsync(string query,CancellationToken token=default)
    {
        if(query.Trim().Length<2)return [];
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(_timeout);token=deadline.Token;
        var body=new {context=new {client=new {clientName="WEB_REMIX",clientVersion="1.20261006.01.00",hl="en",gl="US"}},query=query.Trim(),@params="EgWKAQIIAWoMEA4QChADEAQQCRAF"};
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://music.youtube.com/youtubei/v1/search?prettyPrint=false") { Content=JsonContent.Create(body) };
        using var response=await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(token);
        using var output=new MemoryStream();var buffer=new byte[8192];int read;
        while((read=await stream.ReadAsync(buffer,token))>0) { if(output.Length+read>3_000_000)throw new InvalidDataException("Resposta musical grande demais.");output.Write(buffer,0,read); }
        using var json=JsonDocument.Parse(output.ToArray());return Parse(json.RootElement);
    }
    public static IReadOnlyList<Track> Parse(JsonElement root)
    {
        var result=new List<Track>();
        foreach(var shelf in Find(root,"musicShelfRenderer"))
        {
            var category=Text(shelf,"title");
            if(category.Length>0 && !category.Equals("Songs",StringComparison.OrdinalIgnoreCase))continue;
            foreach(var row in Find(shelf,"musicResponsiveListItemRenderer"))
            {
                if(!row.TryGetProperty("flexColumns",out var columns) || columns.GetArrayLength()<2)continue;
                var name=Text(columns[0].GetProperty("musicResponsiveListItemFlexColumnRenderer"),"text");
                var details=columns[1].GetProperty("musicResponsiveListItemFlexColumnRenderer").GetProperty("text").GetProperty("runs");
                var endpoint=Find(row,"watchEndpoint").FirstOrDefault();
                if(endpoint.ValueKind!=JsonValueKind.Object || !endpoint.TryGetProperty("videoId",out var videoId))continue;
                var id=videoId.GetString()??"";if(id.Length!=11 || !id.All(c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_'))continue;
                var artists=details.EnumerateArray().Where(run=>run.TryGetProperty("navigationEndpoint",out var nav) && nav.TryGetProperty("browseEndpoint",out var browse) && browse.TryGetProperty("browseId",out var bid) && (bid.GetString()??"").StartsWith("UC")).Select(run=>run.GetProperty("text").GetString()).Where(s=>!string.IsNullOrWhiteSpace(s)).ToArray();
                var artist=artists.Length>0?string.Join(", ",artists):details.EnumerateArray().FirstOrDefault().TryGetProperty("text",out var first)?first.GetString()??"YouTube Music":"YouTube Music";
                var thumbnails=Find(row,"thumbnails").SelectMany(v=>v.ValueKind==JsonValueKind.Array?v.EnumerateArray():Enumerable.Empty<JsonElement>()).ToArray();
                var thumbnail=thumbnails.LastOrDefault();var cover=thumbnail.ValueKind==JsonValueKind.Object&&thumbnail.TryGetProperty("url",out var url)?url.GetString()??"":"";
                var durationText=details.EnumerateArray().Select(r=>r.GetProperty("text").GetString()).LastOrDefault(t=>t is not null && System.Text.RegularExpressions.Regex.IsMatch(t,@"^\d+:\d{2}(?::\d{2})?$"));
                TimeSpan? duration=null;if(durationText is not null) { var parts=durationText.Split(':').Select(int.Parse).ToArray();duration=TimeSpan.FromSeconds(parts.Reverse().Select((v,i)=>v*Math.Pow(60,i)).Sum()); }
                if(string.IsNullOrWhiteSpace(name)||result.Any(t=>t.Id==id))continue;
                result.Add(new(id,name,artist,cover,duration,TrackSource.YouTube,"https://www.youtube.com/watch?v="+id));
                if(result.Count==20)return result;
            }
        }
        return result;
    }
    private static string Text(JsonElement node,string property)=>node.TryGetProperty(property,out var value)&&value.TryGetProperty("runs",out var runs)?string.Concat(runs.EnumerateArray().Select(r=>r.GetProperty("text").GetString())):"";
    private static IEnumerable<JsonElement> Find(JsonElement node,string key)
    {
        if(node.ValueKind==JsonValueKind.Object)foreach(var property in node.EnumerateObject()) { if(property.Name==key)yield return property.Value;foreach(var item in Find(property.Value,key))yield return item; }
        else if(node.ValueKind==JsonValueKind.Array)foreach(var child in node.EnumerateArray())foreach(var item in Find(child,key))yield return item;
    }
    public void Dispose()=>_http.Dispose();
}
