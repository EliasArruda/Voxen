using Photino.Blazor;
using Voxen;
using Voxen.Services;
using YoutubeExplode;
using Microsoft.Extensions.DependencyInjection;

// One writer owns the local metadata store; a second launch cannot overwrite another session.
using var instance = new Mutex(true, "Voxen." + Environment.UserName,
    new NamedWaitHandleOptions { CurrentUserOnly = true, CurrentSessionOnly = false }, out var firstInstance);
if (!firstInstance) { Console.WriteLine("Voxen já está aberto neste usuário. Use a janela existente."); return 0; }

var builder = PhotinoBlazorApp.CreateBuilder(args);
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddSingleton<YouTubeService>();
builder.Services.AddSingleton<SoundCloudService>();
builder.Services.AddSingleton<SoundCloudWebService>();
builder.Services.AddSingleton<MusicProviders>();
builder.Services.AddSingleton<ITrackSearchProvider>(services => services.GetRequiredService<MusicProviders>());
// Each search page owns its session; DI does not retain disposed transient sessions.
builder.Services.AddSingleton<Func<SearchSession>>(services =>
    () => ActivatorUtilities.CreateInstance<SearchSession>(services));

builder.Services.AddSingleton<IAudioSourceProvider>(services => services.GetRequiredService<MusicProviders>());
builder.Services.AddSingleton<AudioProxy>();
builder.Services.AddSingleton<NativeAudioService>();
builder.Services.AddSingleton<PlayerService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<LibraryService>();
builder.Services.AddSingleton<ListeningHistoryService>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<PlaybackCoordinator>();

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
