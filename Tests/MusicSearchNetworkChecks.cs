using System.Net;
using Voxen.Services;
internal static class MusicSearchNetworkChecks
{
    public static async Task RunAsync()
    {
        using var stalled=new YouTubeMusicSearchService(new HttpClient(new ResponseHandler(()=>new StreamContent(new StalledStream()))),TimeSpan.FromMilliseconds(60));
        try { await stalled.SearchAsync("artist");throw new Exception("Stalled body should time out"); }
        catch(OperationCanceledException) { Console.WriteLine("PASS Song search bounds response body time after headers arrive"); }
        using var oversized=new YouTubeMusicSearchService(new HttpClient(new ResponseHandler(()=>new ByteArrayContent(new byte[3_010_000]))));
        try { await oversized.SearchAsync("artist");throw new Exception("Oversized body should fail"); }
        catch(InvalidDataException) { Console.WriteLine("PASS Song search stops oversized streamed responses"); }
    }
    private sealed class ResponseHandler(Func<HttpContent> content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=content()});
    }
    private sealed class StalledStream : Stream
    {
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>0;public override long Position{get=>0;set=>throw new NotSupportedException();}
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){await Task.Delay(Timeout.Infinite,token);return 0;}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override void Flush(){}public override long Seek(long o,SeekOrigin so)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
    }
}
