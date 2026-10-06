# Progresso do Voxen

## 2026-10-06 — player funcional e Frosted Studio

- Etapas 1–12 implementadas: Track, busca YouTube debounce/cancel, resultados, fila, player central, fim/avanço, controles, recomendações e autoplay.
- Etapa 13 preparada: provider oficial SoundCloud com OAuth cache/refresh, busca e HLS. Sem credenciais, fonte fica desativada; fixtures HTTP passam. Falta homologação real.
- Redesign autorizado: fundo glass azul/ardósia, gelo, curvas e Studio de três colunas. Review independente: SHIP da superfície compartilhada em Chromium; DESIGN.md e sidecar atualizados.
- Após recusa de instalação GStreamer, reprodução desktop mudou para FFmpeg + SDL3 distribuído pelo NuGet. FFmpeg do sistema é aceito em desenvolvimento; pacote portátil inclui binário verificado.
- Pacote Linux self-contained gerado. `scripts/publish.py` prepara Linux/Windows x64 sem exigir instalação separada de FFmpeg nem SDK .NET no destino.
- Checks determinísticos C#, regressão JS e engine nativo com SDL dummy passaram. Áudio YouTube real passou em Chromium e no Photino/Linux: play, pause, seek, resume, stop. UI testada em 1440/1280/1000/390px, sem overflow horizontal.
- Revisões de código corrigiram races de recomendação tardia, prioridade manual, autoplay desligado após prefetch, callbacks antigos, Stop/Play concorrentes, pause/resume durante seek e entrega de sinais nativos.
- Git: main contém baseline mínimo; implementação em feat/functional-glass-player, commits por incremento, PR de rascunho. Não fazer merge automaticamente.

## Limites e próximo passo

- Homologar API/áudio SoundCloud quando houver credenciais locais.
- CI executa build, checks, testes FFmpeg/SDL dummy e gera pacotes Linux/Windows. Execução gráfica Windows ainda não foi validada.
- Biblioteca, favoritos, playlists persistidas, instaladores e personalização por UI são próximos incrementos. Tokens CSS estão documentados para evolução.
- Não publicar releases nem alterar visibilidade/proteções sem pedido explícito.

## 2026-10-06 — Public SoundCloud increment
- Initial draft PR #1 merged after passing Linux/Windows CI, as explicitly requested.
- Added bounded public website search/audio adapter, with discovered website client identity, six-hour in-memory cache and one refresh on 401/403. Official OAuth remains preferred when configured.
- Public adapter rejects blocked/preview/unsupported encrypted formats; real public search and audio resolution passed without user credentials.
- Fixture checks cover refresh, filtering and HLS normalization. Next: persisted library/history, personalized discovery, keyboard controls and README.

## 2026-10-06 — Library and personalized discovery
- PR #2 merged after Linux/Windows CI and independent review; actual Photino Linux SoundCloud play/pause/seek/resume/stop passed without user credentials.
- Local saved tracks, favorites, playlist create/rename/delete/membership/playback and qualified history added. Atomic writes, bounded metadata, corrupt-file preservation and per-user single-instance protection.
- Recommendations rank artist/title affinity, favorite/repeat/recency signals, exclude last 20 listens and queue, suppress duplicate recordings and limit repeated artists.
- Reproduced failing toggle-after-ended regression before fixing. Discovery now activates after queue end and gives immediate state feedback; manual queue and stop continue to win.
- Qualified listening excludes large and repeated small seeks. Tests include invalid store formats/nulls, persistence, history and recommendation ranking.
- Chromium fixture flows passed library actions, collection playback, discovery after end and widths 1440/1280/1000/390. Native FFmpeg/SDL regression passed. Next: keyboard shortcuts, polished README and final packages.

## 2026-10-06 — Keyboard workflow
- PR #3 merged after passing Linux/Windows CI and independent code/UI review (SHIP for shared Chromium surfaces).
- Added focused-window search, transport, seek, volume/mute, favorite/save, queue and navigation shortcuts plus nonmodal help.
- Explicit guards preserve typing, composition and native focused button behavior. Escape respects inputs; closing help restores previous focus. Search focus settles after route heading focus.
- Node mapping regression and browser flows passed: Ctrl+K focus, typing protection, pause, volume/mute, favorite/save, navigation, help/focus return/Escape and mobile queue drawer.
- README restructuring and refreshed portable packages are next.

## 2026-10-06 — Studio guide and delivery
- PR #4 merged after Linux/Windows build, deterministic/Node/native checks and independent code/UI review.
- Professional Portuguese README covers installation, library, recommendation signals, shortcuts, integration limits, data storage and contribution. Includes a fresh real SoundCloud search/playback capture and vector brand mark.
- Portable packages include documentation and its local image assets. Final Linux packaging and single-instance smoke test follow before delivery.
