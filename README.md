<div align="center">

# Voxen

**Your music. Both sources. One space.**

A desktop music player for YouTube and SoundCloud, with a personal library and an atmosphere that follows your music.

![Voxen desktop player](docs/images/voxen-studio.png)

</div>

## What you can do

- **Find your next song.** Search both sources at once, or choose YouTube or SoundCloud. YouTube search focuses on the YouTube Music song catalog.
- **Make it yours.** Save tracks, mark favorites, create playlists, and revisit recently played music.
- **Keep listening.** Build a queue, skip whenever you like, and enable *Keep discovering* for recommendations based on your listening and favorites.
- **Shape your sound.** Choose Original, Bass boost, Voice, or Brightness, or adjust bass, midrange, and treble yourself.
- **Set the mood.** Cover colors tint the interface while you listen. Turn them off to return to Summer Sky.
- **Choose your language.** Portuguese, English, and Spanish are available in Settings.

## Start listening

1. Download the portable package for your system from a successful [build's artifacts](https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml). A GitHub account is required to download artifacts.
2. Extract the whole folder and open **Voxen** on Linux or **Voxen.exe** on Windows. Keep the included files together.
3. Open **Search**, enter a song or artist, and press **Listen now**.
4. Use the heart for favorites, the bookmark to save, and **+** to add a track to the queue.
5. Open **Library** to create playlists. Open **Settings** to change language, sound, and cover colors.

Use **Space** to pause/resume, **N / P** for next/previous, **M** to mute, and **Ctrl + K** to search. Press **?** for all shortcuts. Typing fields keep their normal behavior.

## A few things to know

Voxen streams public tracks and needs an internet connection. Availability depends on the source, region, and track permissions. SoundCloud search targets tracks and filters obvious spoken content, but provider metadata cannot guarantee every result is music.

Your library and settings stay on your computer. Saving a track saves its details, not an offline audio file. No music-service account or personal API key is needed for public playback.

The portable package includes its audio engine. Linux needs WebKitGTK 4.1 and a working audio device; Windows needs WebView2. If a track is unavailable, try another result or source.
