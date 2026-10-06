# Voxen

## Register
Product: a desktop music player, with search as the first usable increment.

## Platform
Blazor in PhotinoX's native system WebView. Linux and Windows initially.

## Users and purpose
Music listeners who want a lightweight, customizable alternative to Spotify/YouTube Music.
Find music quickly without unnecessary memory use or network requests.

## Design direction
Confirmed by the user: apply the WaveMix Studio design from Stitch (Universal Search & Studio Player).
Dark studio surfaces, mint accent #66f2b1, local Geist/Inter typography, compact track lists,
left navigation, responsive queue inspector and persistent 88px player deck.
Restrained controls, readable track information, keyboard access and accessible contrast.

## Principles
- Native WebView; no embedded Electron or Chromium.
- Keep search metadata independent of the music provider.
- Prefer small components, simple services and bounded results.
- Show honest loading, error and empty states. Do not imply playback before it exists.
- Avoid decorative animations and extra UI dependencies.
