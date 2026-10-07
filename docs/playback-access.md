# Access recovery and transparent album glass

Completed October 7, 2026.

## Report and evidence

The user reported source-access denials on every selected song. The identical denial was not reproduced locally: sixteen public YouTube/SoundCloud results returned audio bytes, and four tracks reached `Playing` through the complete preparation/proxy/FFmpeg/SDL path. This evidence establishes working samples; it does not establish the cause of the user's failure.

## Changes

- Source HTTP 401/403 gets one fresh resolution within the original startup deadline. Preparation metadata is invalidated.
- YouTube parser/cipher state is refreshed both after metadata access failure and through the existing decoder retry. HTTP connections stay pooled.
- Asynchronous decoder failures retain only recognized HTTP status categories. Local diagnostics contain timestamp, provider, phase, exception type, status and retry state, without titles, URLs, stderr, or credentials. Log size is bounded.
- Root/body canvas is transparent. Sidebar, topbar, content, inspector, dock, timeline, controls, labels and source indicators share artwork tokens. Native transparency is enabled on Linux/macOS; Windows keeps its native window frame because Photino requires chromeless Windows windows for native transparency.

## Verification

Release build, application/native checks, JavaScript checks, and independent review passed. Regression coverage verifies one denial retry, successful recovery, persistent-denial termination, underlying-source invalidation after decoder failure, HTTP-category extraction and complete theme reset. Chromium verified that six main surfaces change between red and blue artwork palettes, root/body have zero-alpha backgrounds, public playback works, and 390px layout does not overflow.

The user confirmed successful playback in the reopened, updated Linux package. The exact original upstream denial was not reproduced in probes. If a provider refusal returns, inspect `playback-diagnostics.log` in the local Voxen data directory to identify source, phase, status and retry; do not claim the upstream refusal is resolved without that evidence.
