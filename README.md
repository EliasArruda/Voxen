<div align="center">
  <img src="docs/images/voxen-mark.svg" alt="Voxen" width="80" />
  <h1>Voxen</h1>
  <p><strong>Your next sound. Your own space.</strong></p>
  <p>A desktop music player for Linux and Windows.<br />YouTube and SoundCloud search, a local library, and discoveries shaped by your listening.</p>
  <p>
    <a href="https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml"><img src="https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml/badge.svg" alt="Build and tests" /></a>
    <img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET 10" />
    <img src="https://img.shields.io/badge/platforms-Linux%20%7C%20Windows-f4bd91" alt="Linux and Windows" />
  </p>
  <p><a href="#getting-started">Getting started</a> · <a href="#features">Features</a> · <a href="#keyboard-shortcuts">Shortcuts</a> · <a href="#development">Development</a></p>
</div>

![Voxen Summer Sky interface](docs/images/voxen-studio.png)

Voxen brings search, a local library, and streaming playback into a Summer Sky workspace: illustrated skies, translucent blue surfaces, and soft peach controls. The interface runs in the system WebView; **FFmpeg and SDL3 handle audio independently of WebView codecs**. No Electron or bundled Chromium. Saving a track stores metadata, not an offline audio copy.

## Features

- **Combined search:** YouTube and SoundCloud results arrive progressively. Choose `Todos`, `Youtube`, or `Soundcloud`. Initial suggestions offer starting points.
- **Native player:** play/pause, previous/next, seeking, volume, mute, and a queue of up to 200 entries. The timeline fills as playback advances. Volume responds while dragging without playback updates resetting the slider.
- **Next at any time:** advance through queued tracks immediately. At the end of the queue, request another recommendation without waiting for the current track to finish. If none is available, the current track keeps playing.
- **Local library:** saved tracks, favorites, and playlists you can create, rename, edit, and play.
- **Personal discoveries:** recommendations reflect artists, favorites, listening frequency, and recency. Reasons appear alongside results.
- **Continue discovering:** optional autoplay after the queue. Manual choices take priority.
- **Summer Sky:** sourced wallpaper, rounded glass panels, responsive queue drawer, cover tint, and reduced-motion support.

## Getting started

Download the matching portable artifact from [GitHub Actions](https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml), extract it, and launch Voxen:

```sh
# Linux
chmod +x Voxen
./Voxen
```

On Windows, run `Voxen.exe`.

Packages include .NET, SDL3, and FFmpeg. A graphical session, audio output, and native WebView are required: WebKitGTK on Linux or WebView2 on Windows. Native audio does not require GStreamer plugins. CI artifacts remain available for 14 days; there is no stable installer or release yet.

Open **Buscar** and select a source. Search for a track or artist, or follow a suggestion. Click **Ouvir agora** or a row's play button. Use **+** to queue a track, the heart to favorite it, and the bookmark to save it. The volume icon toggles mute and restores the last nonzero level. Interface labels remain in Portuguese.

### Playback startup

Search highlights, focused/hovered play buttons, the next queued track, and autoplay candidates can prepare stream metadata before playback. Preparation caches at most eight entries for 90 seconds and does not download full tracks. Failed addresses are discarded.

Startup has a 20-second budget from click to first audio. A failed native startup may refresh metadata once within that original budget. Live YouTube radios use audio-only HLS; browser manifests refresh while playback continues. Native provider URLs preserve HTTP range seeking. Timing and availability depend on the source and network.

A Linux sample started six YouTube tracks in 1.5–8.2 seconds and 23 SoundCloud tracks in 0.7–2.9 seconds. Prepared tracks can start sooner. These measurements are not performance guarantees.

## Recommendations

Listening history records qualified continuous playback, not seeks or paused updates. Discovery combines artist/title affinity, favorites, repetition, and recency. Candidates come from up to three artist references; the last 20 listened tracks and queued entries are excluded. Duplicate recordings are filtered, and each artist contributes at most two recommendations.

Autoplay is opt-in. It prepares a candidate before the queue ends, can start when enabled after a track ends, and respects manual additions, stop, and cancellation. Manual **Next** works independently of autoplay.

## Keyboard shortcuts

Shortcuts work while Voxen has focus. Text entry and composition retain normal typing. Outside text fields, **Space only pauses/resumes music**, including when a button or slider has focus. Slider arrow keys retain native behavior. Open the keyboard button for help.

| Action | Shortcut |
| --- | --- |
| Focus search | `Ctrl + K` or `/` |
| Pause/resume | `Space` |
| Next/previous | `N` / `P` |
| Seek forward/back 5 seconds | `Right` / `Left` |
| Raise/lower volume by 5% | `Up` / `Down` |
| Mute/restore volume | `M` |
| Favorite current track | `F` |
| Save current track | `B` |
| Toggle queue | `Q` |
| Library/home | `Alt + L` / `Alt + H` |
| Show help | `?` |
| Close help/queue | `Esc` |

## SoundCloud

Public SoundCloud search and playback work **without personal API credentials**. The website adapter discovers the public client identifier, caches it temporarily, and uses the same native audio engine as YouTube. It does not log in or store your account cookies.

This integration is unofficial: internal endpoints can change and require an update. Blocked tracks, previews, and protected formats are not offered as full playback. Source restrictions still apply.

To use the official API, set both environment variables before launch:

| Variable | Purpose |
| --- | --- |
| `VOXEN_SOUNDCLOUD_CLIENT_ID` | Authorized application identifier |
| `VOXEN_SOUNDCLOUD_CLIENT_SECRET` | Authorized application secret |

The official integration takes priority when both exist. Do not place credentials in code, commits, or distributed packages. `.env` files are not loaded automatically. Official OAuth/refresh and HLS paths are covered by HTTP fixtures; real credential-free tests use the public adapter.

## Your data

Only metadata persists locally:

- Linux: typically `~/.local/share/Voxen/library.json`.
- Windows: `%LOCALAPPDATA%\Voxen\library.json`.

A per-user single-instance lock protects writes. Corrupt or unsupported library files are preserved and reported. The queue is session-only. Clear listening history through the library. There is no account synchronization.

Search and audio requests go to their respective providers; the local listening profile is not uploaded as an external account. Temporary stream addresses do not appear in UI error messages.

## Development

Requires the **.NET 10 SDK**, FFmpeg on `PATH`, and native WebView dependencies. Node runs JavaScript checks; Python 3 packages portable builds. CSS, scripts, and local fonts are already committed.

```sh
git clone https://github.com/EliasArruda/Voxen.git
cd Voxen
dotnet run
```

FFmpeg discovery checks `VOXEN_FFMPEG_PATH`, then the bundled `tools/ffmpeg` executable, then `PATH`.

### Verification

```sh
dotnet build --configuration Release
dotnet run --project Tests/Voxen.Checks.csproj --configuration Release
npm test
SDL_AUDIODRIVER=dummy dotnet run --project Tests/Voxen.Checks.csproj --configuration Release -- --native
```

In PowerShell, set `$env:SDL_AUDIODRIVER = 'dummy'` before the native check command. Optional external-source checks use `--live` and require internet access.

CI builds, runs deterministic .NET/JavaScript checks, packages portable artifacts, and exercises FFmpeg/SDL using dummy audio output on Linux and Windows. Real provider playback was tested on Linux; shared UI flows are tested in Chromium. Windows GUI verification on a physical device remains outstanding.

### Portable builds

```sh
python scripts/publish.py --rid linux-x64 --output artifacts/Voxen-linux-x64
python scripts/publish.py --rid win-x64 --output artifacts/Voxen-win-x64
```

The script publishes a self-contained application and bundles a pinned FFmpeg build after SHA-256 verification. Downloads are cached locally outside Git. FFmpeg license and source information accompany the package.

## Architecture

| Component | Responsibility |
| --- | --- |
| `SearchSession` | Debounce, cancellation, progressive results, stale-response guards |
| `MusicProviders` | YouTube and official/public SoundCloud routing |
| `AudioPreparationService` | Bounded metadata cache and shared resolution |
| `PlayerService` | Playback state, startup budget, recovery, volume/mute |
| `NativeAudioService` | One FFmpeg process, bounded PCM, SDL output |
| `AudioProxy` | Session-only loopback fallback |
| `QueueService` / `PlaybackCoordinator` | Queue, manual next, autoplay, cancellation |
| `LibraryService` / `ListeningHistoryService` | Local metadata and qualified listens |
| `RecommendationService` | Affinity, exclusions, and diversity |

The fallback proxy listens only on `127.0.0.1` with an ephemeral port. Full audio files are not saved to disk. The test browser preview uses an HTML5 backend with hls.js; the desktop uses native audio.

## Contributing

Keep changes focused and open a pull request with the problem, behavior change, and validation. Do not commit credentials, dependencies, binaries, logs, or temporary output. Provider changes need fixture coverage; playback and UI changes need relevant interaction checks.

## Credits

Summer Sky adapts the selected [SpicetifyCat](https://github.com/Adrien5902/SpicetifyCat) reference. Wallpaper provenance and the source repository's MIT notice accompany the asset in `wwwroot/Images`.

[Blazicons.FontAwesome](https://github.com/kyleherzog/Blazicons.FontAwesome) provides regular/solid SVG controls. The official Font Awesome `fa-soundcloud` brand SVG is distributed under CC BY 4.0 with its attribution/license preserved. Geist, Inter, hls.js, SDL3, and FFmpeg retain their respective notices.
