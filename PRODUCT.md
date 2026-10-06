# Voxen

## Register
Desktop music player with live search, playback, queue and optional autoplay.

## Platform
C# .NET 10, Blazor in PhotinoX native system WebView. Audio uses FFmpeg + SDL3, independent of WebKit codecs. Linux and Windows initially. No embedded Electron/Chromium.

## Users and purpose
Music listeners who want a lightweight and customizable alternative to Spotify/YouTube Music.
Find and play music quickly while keeping metadata, queue and network requests bounded.

## Design direction
User selected SpicetifyCat Summer Sky as the replacement visual reference, retaining frosted glass and rounded forms.
Illustrated summer sky, translucent blue surfaces, soft peach accent, local Geist/Inter typography, slim icon navigation, centered search shortcut, compact track rows, responsive queue inspector and persistent player.
See `.impeccable/surfaces/studio.md` for the direction contract.

## Principles
- Native WebView UI and one native audio stream; FFmpeg + bundled SDL3, no full-track downloads or GStreamer requirement.
- Provider-independent metadata; simple services and small components.
- Real controls and honest loading, error and empty states.
- Queue is session-only; autoplay is opt-in, reacts when enabled after queue end, and respects manual choices.
- Public SoundCloud website search and FFmpeg playback work without user credentials. Configured official OAuth is preferred. Internal public endpoints may change. Live Linux playback was validated.
- Saved tracks, favorites, playlists and qualified listening history persist as metadata in a bounded local store. A per-user single-instance lock protects writes. Recommendations use local artist/title affinity, recency and diversity.
