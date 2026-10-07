# Music browsing and source startup

Completed October 7, 2026.

Home uses compact quick picks and horizontal cover shelves. Explore replaces the search navigation label, while `/search` stays compatible. Both views use actual track metadata, mood queries, localized text, and locally bundled Roboto. Settings uses the Font Awesome cog. The English README keeps the logo, badges, product preview, features, and listening instructions.

Artwork drives the background, glass panels, hover and selection surfaces, controls, and foreground tokens. Disabling cover colors restores Summer Sky. Provider icons and errors retain their semantic colors.

## Startup investigation

YoutubeExplode 6.6.2 validates each manifest format, including video streams, before returning an audio-only selection. The audio client now retains audio adaptive formats in public player metadata before that validation. Signatures, playability checks, audio validation, and live/non-audio fallbacks remain upstream responsibilities.

Two public YouTube results for `Daft Punk Get Lucky` resolved in 3,680 / 5,971 ms before and 1,050 / 505 ms after. A separate verification resolved them in 714 / 642 ms and read audio bytes from both. SoundCloud resolved in 569 / 727 ms in that verification and returned readable audio. Measurements cover source resolution, not guaranteed end-to-end startup; network, cache, region, and track availability vary.

## Verification

- Release build; application checks; native FFmpeg/SDL playback and retry checks; JavaScript audio, keyboard, volume, tone, and theme checks.
- Chromium: Home, Explore, both-source search, settings/language changes, public playback, artwork tokens, cog dimensions, queue before playback, and 1440 / 1240 / 390 px layouts.
- Design detector: no blocking findings. Independent code review fixes covered palette assignment and inspector grid breakpoints.

Next step: validate future provider changes against public playback and preserve the bounded startup deadline.
