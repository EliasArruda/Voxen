---
name: Voxen — Summer Sky
description: Workspace musical de vidro azul sobre o céu ilustrado Summer Sky.
colors:
  bg: "#101c32"
  surface: "rgb(17 32 55 / .66)"
  raised: "rgb(34 53 77 / .7)"
  hover: "rgb(67 84 106 / .7)"
  ink: "#f3f7ff"
  muted: "#d0dcee"
  primary: "#f4bd91"
  line: "rgb(187 213 245 / .15)"
  youtube: "#ff6666"
  primary-hover: "#ffd4b1"
typography:
  headline:
    fontFamily: 'Geist, Segoe UI, Noto Sans, sans-serif'
    fontSize: 'clamp(30px, 3.4vw, 48px)'
    fontWeight: 700
    lineHeight: 1.12
    letterSpacing: '-.035em'
  title:
    fontFamily: 'Geist, Segoe UI, Noto Sans, sans-serif'
    fontSize: '16px'
    fontWeight: 600
    letterSpacing: '-.01em'
  body:
    fontFamily: 'Geist, Segoe UI, Noto Sans, sans-serif'
    fontSize: '14px'
    lineHeight: 1.5
  label:
    fontFamily: 'Inter, sans-serif'
    fontSize: '10px'
    fontWeight: 500
rounded:
  small: '12px'
  control: '12px'
  container: '18px'
  symbol: '14px'
  navigation: '14px'
  search: '999px'
  tile: '16px'
  pill: '999px'
spacing:
  compact: '8px'
  item: '12px'
  control: '16px'
  section: '24px'
  page: '30px'
components:
  button-primary:
    backgroundColor: '{colors.primary}'
    textColor: '{colors.bg}'
    rounded: '{rounded.pill}'
    padding: '10px 16px'
  button-primary-hover:
    backgroundColor: '{colors.primary-hover}'
  button-secondary:
    backgroundColor: '{colors.raised}'
    textColor: '{colors.ink}'
    rounded: '{rounded.pill}'
    padding: '10px 16px'
  search-field:
    backgroundColor: 'rgb(28 46 66 / .86)'
    textColor: '{colors.ink}'
    rounded: '{rounded.search}'
    padding: '5px 18px'
  track-table:
    backgroundColor: 'transparent'
    rounded: '{rounded.small}'
  nav-active:
    backgroundColor: 'rgb(244 189 145 / .16)'
    textColor: '{colors.primary}'
    rounded: '{rounded.navigation}'
    padding: '12px'
  source-badge:
    backgroundColor: 'rgb(255 0 0 / .09)'
    textColor: '{colors.youtube}'
    rounded: '{rounded.pill}'
    padding: '2px 8px'
  result-highlight:
    backgroundColor: 'rgb(25 46 69 / .67)'
    textColor: '{colors.ink}'
    rounded: '{rounded.tile}'
    padding: '24px'
---

# Design System: Voxen

## Overview

**Creative North Star: "Summer Sky"**

Summer Sky é um workspace de escuta olhando para um céu de verão. O wallpaper ilustrado original de SpicetifyCat, com nuvens, cidade e gato, permanece reconhecível através do vidro azul; controles pêssego e formas arredondadas deixam a experiência suave e legível.

A direção substitui a identidade anterior por escolha explícita do usuário. Este documento registra a cascata final de `wwwroot/Styles/voxen.css`, fontes locais e componentes Blazor da aplicação nativa. O wallpaper é `wwwroot/Images/summer-sky.jpg`; origem e aviso MIT estão em `wwwroot/Images/SOURCE.md` e no arquivo de licença adjacente.

**Key Characteristics:**
- Céu ilustrado visível através de vidro azul.
- Acento pêssego e controles arredondados.
- Trilho de ícones, busca global em cápsula e player persistente.
- Estados de carregamento, erro, vazio e indisponibilidade legíveis.

**The Landscape Rule.** Preserve a paisagem reconhecível entre e através dos painéis; ela é o material visual central deste mundo.

## Colors

### Primary

Pêssego ilumina ações principais, navegação ativa, foco e controles de áudio. O hover clareia a ação; seleções usam pêssego com baixa opacidade, preservando a transparência.

### Neutral

Azul noturno sustenta o fundo. Vidro azul (`surface`), ardósia elevada (`raised`) e ardósia de interação (`hover`) usam transparência real; texto claro e metadados azulados mantêm hierarquia. Bordas translúcidas recortam as superfícies. YouTube conserva vermelho como identificação de fonte; SoundCloud usa variante laranja e busca pública sem credenciais do usuário; OAuth oficial configurado é opcional e preferido.

## Typography

Geist organiza títulos e texto; Inter distingue rótulos e números compactos. Ambas são WOFF2 locais com `font-display: swap`; Geist cobre pesos 400–700 e Inter 400–600. SVGs próprios fornecem ícones.

Página usa headline responsivo, fixado em (30px) até 520px. Seções usam title; ideias usam (24px), destaque de ideias (28–40px) e (30px) no móvel. Título do resultado usa (28px), (22px) até 899px e (17px) até 520px, limitado a duas linhas. Metadados usam (10–13px), durações com números tabulares e parágrafos com entrelinha (1.65), máximo (70ch).

## Layout

Shell com altura `100dvh`, mínimo (450px), trilho de ícones (72px), inspector (288px), header (56px) e player (104px). O header ocupa toda a largura; navegação, conteúdo e inspector ficam abaixo; o player ocupa a base. Padding externo (12px), gaps (10px), conteúdo central com rolagem independente e padding (34px 30px 44px). Busca global centralizada tem largura `min(44%,460px)` e altura (40px).

Até (1279px), inspector vira drawer não modal com fechamento explícito: top (80px), right (12px), bottom (128px). Até (899px), trilho (64px), shell com padding/gap (8px), conteúdo (26px 20px 36px), artista abaixo do título e volume oculto. Até (520px), trilho (48px), shell com padding/gap (6px), conteúdo (24px 12px 30px), player (116px); timeline permanece na segunda linha. Busca global passa ao fluxo normal e oculta a dica de teclado. Drawer tem top (68px), right (6px), bottom (134px), máximo `calc(100vw - 72px)`.

Lista desktop usa grid `30px 36px minmax(0,2fr) minmax(0,1fr) 74px 40px 102px`, gap (8px), altura mínima de faixa (62px). Até (899px), grid `30px 34px minmax(0,1fr) 66px 38px 102px`; até (520px), grid `28px 28px minmax(0,1fr) 90px` mantém reproduzir, capa, título/artista e ações de favoritar, salvar e adicionar. Ideias passam de duas colunas a uma até (620px); recomendações empilham ações no móvel.

## Elevation & Depth

O wallpaper usa `center bottom / cover fixed`, sob véu `rgb(10 23 45 / .36)`. Main tem `rgb(10 25 44 / .56)` e blur (2px). Trilho e inspector usam `rgb(14 28 49 / .64)`, header `rgb(12 26 46 / .48)` e player `rgb(12 27 45 / .8)`, com blur (14px). Drawer aberto usa `rgb(16 31 52 / .96)`. Sem suporte a backdrop-filter, os painéis recebem `rgb(15 29 49 / .94)`.

**The Glass Rule.** Concentre o blur no shell; conteúdo e linhas devem continuar legíveis sem filtros adicionais.

TrackAtmosphere acrescenta capa HTTPS válida, ampliada e desfocada (80px), saturação (1.4), opacidade (.12), sem capturar interação. Após carregar, capas cruzam opacidades (800ms, cubic-bezier(.16,1,.3,1)); ausência de capa ou falha conserva o wallpaper. Inspector sobreposto usa sombra `-10px 0 28px rgb(0 0 0 / .35)`; avisos do player `0 6px 20px rgb(0 0 0 / .2)`. Player (10), inspector (20), atalho de conteúdo (30) e ajuda de atalhos (40) definem a ordem.

## Shapes

Painéis do shell usam container; header, busca e pequenas ações usam pill. Destaques e descoberta usam tile; capas pequenas e botões de ícone usam small. Inspector usa symbol. Linhas de resultados têm cantos (10px); tabela é transparente com raio (12px). Formas legadas da biblioteca e vazios mantêm cantos (24px). Capas recortam imagens com `object-fit: cover`.

## Components

- **Botões:** ação pêssego sobre azul noturno, secundária ardósia com borda; altura mínima (42px), peso (650), texto (12px), padding (10px 16px), cápsula. Ícones usam control; reprodução central circular (38px), branco sobre azul noturno. Foco tem outline pêssego (2px), offset (4px). Controles de áudio indisponíveis usam opacidade (.45).
- **Busca:** vidro mais leve, raio search, padding (5px 18px), input (42px); borda pêssego no foco. Input tem nome acessível e limpar explícito. A fonte padrão é Todas (YouTube + SoundCloud); opções individuais continuam disponíveis. Resultados chegam progressivamente, permanecem utilizáveis enquanto a outra fonte responde e convivem com avisos de falha parcial. Contagem e andamento usam status acessível. Antes da pesquisa, ideias curadas de músicas e ambientes levam a consultas reais; até três artistas distintos do histórico e favoritos locais aparecem como atalhos quando disponíveis. A composição de ideias passa de duas colunas a uma até (620px).
- **Navegação:** itens mínimos (46px), padding (12px); estado ativo usa pêssego translúcido sem borda lateral. Trilho mantém nomes acessíveis.
- **Selo de fonte:** cápsula Inter (10px, peso 550), fundo vermelho translúcido, borda `rgb(255 77 77 / .25)`; seletor de fonte usa estado `aria-pressed` e indisponibilidade explícita.
- **Resultados e descoberta:** containers arredondados; destaque usa vidro `rgb(25 46 69 / .67)`, sem borda, cantos (16px), padding (24px). Linhas têm play, favoritar, salvar e adicionar à fila; coração e marcador usam pêssego com preenchimento translúcido no estado selecionado e `aria-pressed`. Hover das linhas usa `rgb(57 76 99 / .65)`. O destaque revela sua chegada por recorte (400ms, cubic-bezier(.16,1,.3,1)), de inset inferior (8%) ao recorte completo, com cantos (24px). Títulos longos truncam na linha, com texto completo no atributo title.
- **Inspector e player:** capa atual, fila removível e seleção real. Autoplay é opt-in com toggle acessível, prepara um candidato durante a reprodução e continua quando ativado após o fim da fila; entradas manuais têm precedência. Faixa atual usa pêssego translúcido. Dock mantém play/pause, anterior/próxima, timeline e volume; stop desaparece até 899px. Avisos usam status/alert. SoundCloud público está disponível sem credenciais do usuário; a barra lateral identifica integração pública ou API oficial conforme a configuração.

- **Biblioteca:** salvas, favoritos, playlists e histórico de escuta qualificada persistem como metadados locais. Seletores de coleção e playlists usam cápsulas ardósia, contagens discretas e pêssego translúcido com `aria-pressed`. Formulários rotulados criam e renomeiam playlists; exclusão e limpeza de histórico pedem confirmação inline. Estados vazios encaminham à busca.
- **Descobertas pessoais:** listas de recomendações usam surface, hover raised, capas compactas e motivo em pêssego. Afinidade local por artista/título, favoritos e escutas orienta a seleção; as últimas 20 faixas ouvidas são excluídas, duplicatas são filtradas e há limite por artista para diversidade. Atualizar e tentar novamente são ações explícitas.
- **Atalhos:** botão na barra abre ajuda não modal com teclas em Inter sobre raised; oferece busca, transporte, seek, volume, favoritos, salvar, fila e navegação. Atalhos respeitam campos de edição e exigem a janela em foco. O painel mantém fechamento explícito e Escape.

Transições de fundo/cor/borda usam (160ms, ease-out). O link principal das ideias desloca sua seta (4px) no hover com transição (200ms). `prefers-reduced-motion` remove transições, o recorte de chegada e o deslocamento da seta, mantendo rolagem automática. A atmosfera só muda com a capa; não há movimento decorativo contínuo.

## Do's and Don'ts

### Do:
- Do preservar a paisagem Summer Sky, vidro azul e acento pêssego nos estados ativos.
- Do manter foco visível, rótulos acessíveis e redução de movimento.
- Do preservar a timeline utilizável no player móvel e títulos completos acessíveis.

### Don't:
- Don't substituir a paisagem aprovada por gradientes abstratos como identidade principal.
- Don't adicionar blur a cada linha ou animação contínua aos campos de cor.
- Don't ocultar estados de erro das fontes públicas ou prometer reprodução offline a partir dos metadados salvos.

## Player controls refinement

The seek timeline uses a 4px peach filled track with a transparent 1px handle, preserving the native range input and keyboard semantics. Fill reflects playback position, replacing the rainbow track. Volume lives in a client-owned slider: playback rerenders do not overwrite its value during dragging or pending changes; updates are serialized and coalesced. Percent text has a fixed width to prevent layout jitter. The volume button and M share mute/restore state. Outside text fields, Space controls playback even with buttons or sliders focused; slider arrows retain native behavior. Route changes do not focus h1.

Source badges contain only accessible, titled icons. SoundCloud uses the official Font Awesome brand silhouette in orange-yellow #ffbc55; regular/solid volume controls use Blazicons.FontAwesome. Search filters are plain labels Todos, Youtube, Soundcloud. The Summer Sky palette, shell, typography and layout remain unchanged.

### Personalized listening

Settings lives in the main content region, accessed by a gear in the topbar. It preserves the Summer Sky typography, round controls and glass surfaces. Language, three tone bands, four sound presets and a cover-color toggle form separate readable groups. Settings is a page-like surface, not a modal.

The now-playing pane gives the real artwork and title priority, with save, source and artist-search actions. Artist metrics, biographies and credits are omitted when the provider does not supply them. Its memory point is the artwork changing the surrounding atmosphere: a sampled cover hue tints dark surfaces and a pale accent across controls. Text stays light for contrast. Turning cover colors off restores Summer Sky. Existing image crossfades respect reduced motion; no extra looping motion is introduced.

Portuguese, English and Spanish share the same layout. Metadata such as song titles, artists and user playlist names stays in its original language. Search filters retain brand icons beside their names. YouTube search requests the Songs catalog rather than general videos; SoundCloud excludes explicit spoken-content metadata without claiming perfect classification.
