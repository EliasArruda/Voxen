# Personalized listening increment

Completed implementation on October 7, 2026. This extends Summer Sky with product-focused documentation, search filter icons, Settings, Portuguese/English/Spanish resources, persistent local preferences, real-time three-band equalization, presets, artwork-derived colors, and an artwork-led now-playing pane.

YouTube uses the public YouTube Music Songs search endpoint. Audio resolution still uses the existing YouTube player. The adapter does not fall back to general videos. SoundCloud uses its track endpoints with an explicit spoken-content metadata filter; it cannot guarantee perfect classification. These public adapters can change upstream.

Native equalization processes the FFmpeg stereo PCM stream at 48 kHz before SDL output; browser fallback uses matching WebAudio bands and boost headroom. Flat native processing preserves sample bytes. Tone changes do not resolve/reload streams. Settings persist locally with debounced writes while dragging.

Cover images are limited to known provider image hosts, read with byte/time bounds, and decoded through the bundled FFmpeg. Stale requests cannot overwrite newer artwork. Unavailable covers retain the base atmosphere. No fabricated artist biographies or statistics are displayed.

Validation covers preference persistence and malformed files, translation metadata boundaries, frequency response, stereo independence, source filtering, missing search categories, streamed response size/deadline bounds, JavaScript tone graph/lifecycle, existing playback regressions, and native FFmpeg/SDL checks. Chromium exercised language changes, live EQ dragging, actual WebAudio signal, public song playback and cover theming, and desktop/mobile overflow. Interactive Windows GUI validation remains pending on Windows hardware.
