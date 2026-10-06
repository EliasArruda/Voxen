# Playback reliability — 2026-10-06

The failing YouTube sample `rFZHOHl-L8A` is a live radio. Finite-manifest resolution failed with cipher extraction; the audio-only HLS fallback now starts it and refreshes browser media playlists. Native playback reads provider audio URLs directly, preserving remote HTTP range seeks. A startup failure renews preparation once, within the original 20-second deadline. Error text reports safe categories and never includes temporary URLs.

Validation on Linux: Release build, .NET/JavaScript checks, native FFmpeg/SDL tests, bounded retry/deadline/cancellation regressions. A real-source startup sample passed for six YouTube tracks (1.5–8.2 seconds) and 23 SoundCloud tracks (0.7–2.9 seconds). Sustained live playback/playlist refresh and SoundCloud seek to 60 seconds passed. Network/provider timing varies; this sample does not guarantee every public track is playable. Windows graphical testing remains outstanding.

Summer Sky redesign: sourced illustrated wallpaper, soft peach controls, slim navigation, transparent panels, persistent player. Browser checks covered 1440×1000,1000×800 and 390×844 with no horizontal overflow or page errors. Finish review accepted the scored documentation correction. Source license and attribution accompany the wallpaper.

Next: homologate the Windows GUI on a physical device and use concrete failing track URLs for any remaining provider-specific availability reports.
