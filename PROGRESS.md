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
