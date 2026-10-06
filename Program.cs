using Photino.Blazor;
using Voxen;
using Voxen.Services;
using YoutubeExplode;
using Microsoft.Extensions.DependencyInjection;

var builder = PhotinoBlazorApp.CreateBuilder(args);
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddSingleton<YouTubeService>();
builder.Services.AddSingleton<ITrackSearchProvider>(services => services.GetRequiredService<YouTubeService>());
// Each search page owns its session; DI does not retain disposed transient sessions.
builder.Services.AddSingleton<Func<SearchSession>>(services =>
    () => ActivatorUtilities.CreateInstance<SearchSession>(services));

builder.RootComponents.Add<Routes>("#app");

builder.ConfigureMainWindow(window =>
{
    window
        .SetTitle("Voxen")
        .SetSize(1280, 800)
        .Center();
});

await using var app = builder.Build();

return app.Run();
