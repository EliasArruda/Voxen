# Voxen

## Register
Desktop music player with live search, playback, queue and optional autoplay.

## Platform
C# .NET 10, Blazor in PhotinoX native system WebView. Audio uses FFmpeg + SDL3, independent of WebKit codecs. Linux and Windows initially. No embedded Electron/Chromium.

## Users and purpose
Music listeners who want a lightweight and customizable alternative to Spotify/YouTube Music.
Find and play music quickly while keeping metadata, queue and network requests bounded.

## Design direction
User selected WaveMix Studio Search & Studio Player, then requested frosted glass backgrounds and rounder forms.
Deep blue/slate glass, ice-blue accent, local Geist/Inter typography, compact track rows, left navigation, responsive queue inspector and persistent player.
See `.impeccable/surfaces/studio.md` for the direction contract.

## Principles
- Native WebView UI and one native audio stream; FFmpeg + bundled SDL3, no full-track downloads or GStreamer requirement.
- Provider-independent metadata; simple services and small components.
- Real controls and honest loading, error and empty states.
- Queue is session-only; autoplay is opt-in and respects manual choices.
- SoundCloud remains disabled without local environment credentials. Live validation requires those credentials.
- Library, favorites and persisted playlists are future work and labeled accordingly.
