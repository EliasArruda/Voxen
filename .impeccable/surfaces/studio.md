# Voxen Studio

Mode: Operate. Native desktop WebView on Linux and Windows; Chromium preview tests the shared Blazor surface.

## Direction contract

**Direction.** User selected WaveMix Studio Search & Studio Player, then explicitly replaced its charcoal/mint palette with frosted glass and softer rounded shapes. Use deep blue glass with ice blue accent; preserve the three-column studio workflow.

**First viewport.** Sidebar anchors Home/Search; live results dominate the center; current track and queue stay visible at right on large screens. Persistent player exposes transport, seek and volume. No mock catalog or fabricated quality claims in production.

**Material.** Static blue/slate color fields show through translucent shell panels. Bound backdrop blur to shell panels. Main surfaces and cover crops use 18–24px radii; action buttons use pills.

**Type.** Local Geist/Inter; compact labels and clear heading hierarchy. Real track titles truncate in rows and wrap in the inspector; complete title remains accessible.

**Signature interaction.** Search streams metadata into playable results; one click selects a track and updates the persistent player and queue. At narrow widths the queue is a closable nonmodal drawer and the timeline remains usable.

**States and truth.** Idle/loading/empty/error/retry are real. SoundCloud search is disabled without environment credentials. Autoplay is opt-in and prefetches in the final 15 seconds; manual queue entries take precedence. Audio uses FFmpeg + SDL3 independently of WebView codecs; portable packages include FFmpeg. Linux native playback was verified. No graphical Windows or live SoundCloud validation is claimed without evidence.
