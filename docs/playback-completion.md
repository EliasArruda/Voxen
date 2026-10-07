# Playback completion and audio controls

Completed on 2026-10-07.

Native playback compares decoded completion with expected track duration, allowing codec rounding. A significantly truncated stream refreshes its source once and resumes the same track from the heard position. Repeated interruption stops with an error, preserving the queue and retry position. YouTube uses the seekable segmented stream for non-live audio.

The topbar offers four debounced search suggestions with keyboard selection, playback and a link to all results. Audio settings expose seven bands (32 Hz–16 kHz), eight presets and stereo balance, with matching browser/native attenuation and saved preferences.

Validation: native FFmpeg/SDL regression covers early EOF, successful recovery, repeated clean interruption and manual continuation. DSP checks cover sub-bass, air and balance; JavaScript checks cover the seven-band graph and teardown. Chromium checks cover search, keyboard playback, Escape, Ctrl+K, all-results navigation, EQ/reset and desktop/mobile overflow. A public Numb sample decoded 187.5 seconds against 188 seconds expected on both input paths; the user's original premature skip was not reproduced in that sample.

Next: monitor full-song playback with real providers; keep refresh retries bounded.
