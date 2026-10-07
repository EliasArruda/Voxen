<div align="center">
  <img src="docs/images/voxen-mark.svg" alt="Voxen" width="80" />
  <h1>Voxen</h1>
  <p><strong>Your next sound. Your own space.</strong></p>
  <p>A desktop music player for Linux and Windows.<br />Discover music on YouTube and SoundCloud, shape your sound, and build your own library.</p>
  <p>
    <a href="https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml"><img src="https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml/badge.svg" alt="Build and tests" /></a>
    <img src="https://img.shields.io/badge/platforms-Linux%20%7C%20Windows-f4bd91" alt="Linux and Windows" />
    <img src="https://img.shields.io/badge/languages-PT%20%7C%20EN%20%7C%20ES-8bc3da" alt="Portuguese, English and Spanish" />
  </p>
  <p><a href="#what-you-can-do">Features</a> · <a href="#start-listening">Start listening</a> · <a href="#a-few-things-to-know">Good to know</a></p>
</div>

![Voxen listening workspace](docs/images/voxen-studio.png)

## What you can do

- **Find your next song.** Search both sources at once, or choose YouTube or SoundCloud. YouTube search focuses on the YouTube Music song catalog.
- **Make it yours.** Save tracks, mark favorites, create playlists, and revisit recently played music.
- **Keep listening.** Build a queue, skip whenever you like, and enable *Keep discovering* for recommendations based on your listening and favorites.
- **Shape your sound.** Choose Original, Bass boost, Voice, or Brightness, or adjust bass, midrange, and treble yourself.
- **Set the mood.** Artwork colors adapt the entire layout, including the sidebar and player controls. The Linux desktop canvas is transparent. Turn them off to restore the original blue glass.
- **Choose your language.** Portuguese, English, and Spanish are available in Settings.

## Start listening

1. Download the portable package for your system from a successful [build's artifacts](https://github.com/EliasArruda/Voxen/actions/workflows/checks.yml). A GitHub account is required to download artifacts.
2. Extract the whole folder and open **Voxen** on Linux or **Voxen.exe** on Windows. Keep the included files together.
3. Open **Explore**, enter a song or artist, and press **Listen now**.
4. Use the heart for favorites, the bookmark to save, and **+** to add a track to the queue.
5. Open **Library** to create playlists. Open **Settings** to change language, sound, and cover colors.

Use **Space** to pause/resume, **N / P** for next/previous, **M** to mute, and **Ctrl + K** to search. Press **?** for all shortcuts. Typing fields keep their normal behavior.

## A few things to know

Voxen streams public tracks and needs an internet connection. Availability depends on the source, region, and track permissions. SoundCloud search targets tracks and filters obvious spoken content, but provider metadata cannot guarantee every result is music.

Your library and settings stay on your computer. Saving a track saves its details, not an offline audio file. No music-service account or personal API key is needed for public playback.

The portable package includes its audio engine. Linux needs WebKitGTK 4.1 and a working audio device; Windows needs WebView2. If a track is unavailable, try another result or source.
