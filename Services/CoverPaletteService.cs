using System.Diagnostics;
namespace Voxen.Services;

public sealed record CoverPalette(int R, int G, int B);
public sealed class CoverPaletteService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CoverPalette> _cache = new();
    public async Task<CoverPalette?> ExtractAsync(string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme != "https"
            || !(uri.Host.EndsWith(".ytimg.com",StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".sndcdn.com",StringComparison.OrdinalIgnoreCase) || uri.Host is "yt3.googleusercontent.com" or "lh3.googleusercontent.com")) return null;
        if (_cache.TryGetValue(url,out var cached)) return cached;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(6));
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        using var response = await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,deadline.Token); response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var bytes = new MemoryStream();var buffer=new byte[8192];int count;
        while ((count=await source.ReadAsync(buffer,deadline.Token))>0) { if(bytes.Length+count>2_000_000)return null;bytes.Write(buffer,0,count); }
        var start = new ProcessStartInfo(NativeAudioService.FindFfmpeg()) { RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true };
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-threads","1","-i","pipe:0","-vf","scale=16:16","-frames:v","1","-f","rawvideo","-pix_fmt","rgb24","pipe:1"}) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        using var kill = deadline.Token.Register(()=>{try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}});
        var errors = process.StandardError.ReadToEndAsync(deadline.Token);
        using var colors = new MemoryStream();
        var read = process.StandardOutput.BaseStream.CopyToAsync(colors,deadline.Token);
        await process.StandardInput.BaseStream.WriteAsync(bytes.ToArray(),deadline.Token);process.StandardInput.Close();
        await Task.WhenAll(read,process.WaitForExitAsync(deadline.Token),errors);
        if(process.ExitCode!=0)return null;
        var palette = Select(colors.ToArray()); if(palette is null)return null;
        if(_cache.Count>=64)_cache.Clear();_cache[url]=palette;return palette;
    }
    public static CoverPalette? Select(byte[] rgb)
    {
        var groups = new Dictionary<int,(double Weight,double R,double G,double B)>();
        for(var i=0;i+2<rgb.Length;i+=3)
        {
            double r=rgb[i],g=rgb[i+1],b=rgb[i+2];var max=Math.Max(r,Math.Max(g,b));var min=Math.Min(r,Math.Min(g,b));
            if(max<35 || min>225)continue;
            var weight=1+(max-min)/70;var key=((int)r/48<<8)|((int)g/48<<4)|((int)b/48);
            groups.TryGetValue(key,out var v);groups[key]=(v.Weight+weight,v.R+r*weight,v.G+g*weight,v.B+b*weight);
        }
        if(groups.Count==0)return rgb.Length>=3?new CoverPalette(rgb[0],rgb[1],rgb[2]):null;var winner=groups.Values.MaxBy(v=>v.Weight);
        return new((int)(winner.R/winner.Weight),(int)(winner.G/winner.Weight),(int)(winner.B/winner.Weight));
    }
}
