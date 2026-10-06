using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Voxen.Services;
// A session-only loopback endpoint lets the native audio element seek without downloading whole tracks.
public sealed class AudioProxy : IAsyncDisposable
{
    private readonly SemaphoreSlim _startup = new(1);
    private readonly string _secret = Guid.NewGuid().ToString("N");
    private WebApplication? _app;
    private AudioResource? _resource;
    private long _version;
    private string _address = "";
    public async Task<string> PublishAsync(AudioResource resource, long version, CancellationToken token)
    {
        await _startup.WaitAsync(token);
        try
        {
            if (_app is null)
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.Logging.ClearProviders();
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                var app = builder.Build();
                app.MapGet("/{secret}/audio/{version:long}", async (string secret, long version, HttpContext context) =>
                {
                    var current = _resource;
                    if (secret != _secret || version != _version || current is null) return Results.NotFound();
                    try
                    {
                        var stream = await current.OpenAsync(context.RequestAborted);
                        context.Response.Headers.CacheControl = "no-store";
                        context.Response.Headers.AccessControlAllowOrigin = "*";
                        return Results.Stream(stream, current.ContentType, enableRangeProcessing: stream.CanSeek);
                    }
                    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { return Results.StatusCode(499); }
                    catch (Exception) { return Results.StatusCode(502); }
                });
                await app.StartAsync(token);
                _app = app;
                _address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            }
            _resource = resource; _version = version;
            return $"{_address}/{_secret}/audio/{version}";
        }
        finally { _startup.Release(); }
    }
    public void Clear() { _resource = null; }
    public async ValueTask DisposeAsync()
    {
        Clear();
        if (_app is not null) await _app.DisposeAsync();
        _startup.Dispose();
    }
}
