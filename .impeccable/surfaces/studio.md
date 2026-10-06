# Voxen Summer Sky

Mode: Operate. Native desktop WebView on Linux and Windows; Chromium preview validates the shared Blazor surface.

## Direction contract

**Direction and seed.** The user explicitly chose “Summer Sky — céu azul e tons suaves” from SpicetifyCat, replacing Frosted Studio. The seed is a listening workspace overlooking a summer sky: sourced clouds/city/cat landscape remains visible through blue glass, with soft peach controls. Reference: https://github.com/Adrien5902/SpicetifyCat/tree/main/themes/Summer_Sky. User prefers rounded forms. Preserve Voxen product behavior rather than Spotify-specific controls.

**First viewport.** A slim 72px icon navigation rail, centered search shortcut in the global pill header, large readable page heading, broad search/results area, right now-playing inspector at 1280px and above, and persistent player. At narrower widths, queue opens as a closable nonmodal drawer. Timeline stays usable on mobile.

**Material and palette.** Original Summer Sky wallpaper, attributed beside the asset. Blue glass grounded in #101c32, peach #f4bd91, white #f3f7ff and secondary #d0dcee. Main glass has 2px blur and 56% dark opacity; shell glass uses 14px blur with stronger opacity. Shell radii18px; content radii12–16px; search and small actions use pills. Subtle cover tint changes with selected track; reduced-motion preference disables transitions.

**Type.** Local Geist/Inter. Main headings30–48px, compact metadata and clear controls. Row titles truncate with complete accessible labels; inspector titles wrap. No decorative labels above headings.

**Signature interaction.** Search combines YouTube and SoundCloud; suggestions immediately give starting points. Playback updates dock, cover atmosphere and queue. Favorite/playlists shortcuts lead to actual local collections. Keyboard behavior and focus remain usable.

**Quality bar.** Compare directly to the supplied Summer Sky preview: recognizable illustrated background, transparent open listening layout, slim icon rail, centered pill search, soft peach controls, large cover/now-playing area. Preserve adequate contrast, actual content, accessible controls and no horizontal overflow at390×844,1000×800,1440×1000. No generated comp or alternate visual world is approved.

**States and truth.** Idle/loading/empty/error/retry are real. Public SoundCloud adapter works without personal credentials; official OAuth is optional. Local library/favorites/playlists/history persist; queue is session-only. Recommendations use local affinity and exclude recent listens. Autoplay is opt-in and manual queue entries take priority. FFmpeg/SDL3 provide native streaming independently of WebView codecs. Live YouTube uses audio-only HLS; failed startup can refresh metadata once within the original20-second budget. Linux playback is tested; no graphical Windows validation is claimed.
