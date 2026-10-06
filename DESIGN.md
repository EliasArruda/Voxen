---
name: Voxen — Frosted Studio
description: Estúdio musical de vidro azul, compacto e orientado à busca.
colors:
  bg: "#0b1221"
  surface: "rgb(22 34 53 / .75)"
  raised: "rgb(39 55 77 / .72)"
  hover: "rgb(62 82 107 / .65)"
  ink: "#f3f7ff"
  muted: "#bbc9de"
  primary: "#b9deff"
  line: "rgb(187 213 245 / .15)"
  youtube: "#ff6666"
  primary-hover: "#d5ebff"
typography:
  headline:
    fontFamily: 'Geist, Segoe UI, Noto Sans, sans-serif'
    fontSize: '27px'
    fontWeight: 650
    lineHeight: 1.3
    letterSpacing: '-.02em'
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
  container: '24px'
  symbol: '16px'
  navigation: '14px'
  search: '18px'
  tile: '20px'
  pill: '999px'
spacing:
  compact: '8px'
  item: '12px'
  control: '16px'
  section: '24px'
  page: '28px'
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
    backgroundColor: 'rgb(22 34 53 / .55)'
    textColor: '{colors.ink}'
    rounded: '{rounded.search}'
    padding: '4px 14px'
  track-table:
    backgroundColor: '{colors.surface}'
    rounded: '{rounded.container}'
  nav-active:
    backgroundColor: 'rgb(185 222 255 / .12)'
    textColor: '{colors.primary}'
    rounded: '{rounded.navigation}'
    padding: '10px 13px'
  source-badge:
    backgroundColor: 'rgb(255 0 0 / .09)'
    textColor: '{colors.youtube}'
    rounded: '{rounded.pill}'
    padding: '2px 8px'
  result-highlight:
    textColor: '{colors.ink}'
    rounded: '{rounded.container}'
    padding: '20px'
---

# Design System: Voxen

## Overview

**Creative North Star: "Frosted Studio"**

Frosted Studio mantém a organização WaveMix Studio: navegação lateral, busca central, inspector de faixa e fila, e player persistente. Vidro translúcido azul/ardósia, campos de cor radiais e uma atmosfera discreta derivada da capa atual dão profundidade às formas arredondadas sem disputar atenção com títulos e metadados.

O usuário substituiu explicitamente carvão/menta por vidro e curvas suaves. Este documento captura a cascata efetiva de `wwwroot/Styles/voxen.css`, fontes locais e componentes Blazor; a referência WaveMix define a topologia, não a paleta atual.

**Key Characteristics:**
- Vidro azul com acento gelo.
- Listas compactas e ações de reprodução explícitas.
- Navegação lateral, inspector responsivo e player persistente.
- Estados de carregamento, erro, vazio e indisponibilidade legíveis.

## Colors

### Primary

Gelo ilumina ações principais, navegação ativa, foco e controles de áudio. O hover clareia a ação; seleções usam gelo com baixa opacidade, preservando a transparência.

### Neutral

Azul noturno sustenta o fundo. Vidro azul (`surface`), ardósia elevada (`raised`) e ardósia de interação (`hover`) usam transparência real; texto claro e metadados azulados mantêm hierarquia. Bordas translúcidas recortam as superfícies. YouTube conserva vermelho como identificação de fonte; SoundCloud usa variante laranja e busca pública sem credenciais do usuário; OAuth oficial configurado é opcional e preferido.

## Typography

Geist organiza títulos e texto; Inter distingue rótulos e números compactos. Ambas são WOFF2 locais com `font-display: swap`; Geist cobre pesos 400–700 e Inter 400–600. SVGs próprios fornecem ícones.

Página usa headline, reduzido para (23px) no móvel. Seções usam title; títulos de faixa usam (12px, peso 550). Metadados usam (10–11px), com números tabulares nas durações. Parágrafos usam entrelinha (1.65), máximo (70ch). Destaque inicial usa (38px), reduzido para (32px) até 899px. Destaque de resultado usa (23px), (19px) no trilho e (17px) no móvel; título limitado a duas linhas.

## Layout

Shell de três colunas com altura `100dvh`, mínimo (450px), navegação (216px), inspector (288px), barra (56px) e player (104px). Padding e gaps externos (12px) deixam o fundo aparecer entre painéis. Conteúdo central rola independentemente; largura máxima (1400px), padding (24px 20px 40px).

Até (1279px), inspector vira drawer não modal acionado por botão: top (80px), right (12px), bottom (128px). Até (899px), navegação vira trilho (64px), shell usa padding/gap (8px), artista passa abaixo do título e volume desaparece. Até (520px), trilho (48px), shell (6px), player (116px); timeline ocupa uma segunda linha. Drawer usa top (68px), right (6px), bottom (134px), máximo `calc(100vw - 72px)`.

Lista desktop usa grid `30px 36px minmax(0,2fr) minmax(0,1fr) 74px 40px 102px`, gap (8px), altura mínima de faixa (62px). Até (899px), grid `30px 34px minmax(0,1fr) 66px 38px 102px`; até (520px), grid `28px 28px minmax(0,1fr) 90px` mantém reproduzir, capa, título/artista e ações de favoritar, salvar e adicionar; fonte e duração saem da linha. Descoberta passa de duas colunas a uma; recomendações empilham as ações abaixo da faixa no móvel.

## Elevation & Depth

Gradientes radiais azul/ardósia aparecem através dos painéis. TrackAtmosphere acrescenta a capa HTTPS da faixa atual, ampliada e desfocada (80px), saturação (1.4), opacidade (.22), atrás do shell e sem capturar interação. Após carregar a nova imagem, as capas anterior e atual cruzam suas opacidades (800ms, cubic-bezier(.16,1,.3,1)); ausência de capa válida ou falha de carregamento conserva o fundo base. Blur de fundo (18px) é restrito à navegação, barra, inspector e player. Main usa fundo sutil; cards usam transparência e borda, sem blur próprio. Sem suporte a `backdrop-filter`, os painéis recebem fundo sólido (`#18283d`).

Inspector sobreposto usa sombra `-10px 0 28px rgb(0 0 0 / .35)`. Avisos do player usam `0 6px 20px rgb(0 0 0 / .2)`. Ordem: player (10), inspector (20), atalho de conteúdo (30).

## Shapes

Painéis e containers principais usam container; barra e descoberta usam tile. No móvel, painéis do shell usam (19px). Busca e capa de destaque usam search; navegação usa navigation; capas pequenas e botões de ícone usam small. Ações e selos usam pill; reprodução principal é circular. Capas recortam imagens com `object-fit: cover`.

## Components

- **Botões:** ação gelo sobre azul noturno, secundária ardósia com borda; altura mínima (42px), peso (650), texto (12px), padding (10px 16px), cápsula. Ícones usam control; reprodução central circular (38px). Foco tem outline gelo (2px), offset (4px). Controles de áudio indisponíveis usam opacidade (.45).
- **Busca:** vidro mais leve, raio search, padding (4px 14px), input (42px); borda gelo no foco. Input tem nome acessível e limpar explícito. A fonte padrão é Todas (YouTube + SoundCloud); opções individuais continuam disponíveis. Resultados chegam progressivamente, permanecem utilizáveis enquanto a outra fonte responde e convivem com avisos de falha parcial. Contagem e andamento usam status acessível. Antes da pesquisa, ideias curadas de músicas e ambientes levam a consultas reais; até três artistas distintos do histórico e favoritos locais aparecem como atalhos quando disponíveis. A composição de ideias passa de duas colunas a uma até (620px).
- **Navegação:** itens mínimos (42px), padding (10px 13px); estado ativo usa gelo translúcido sem borda lateral. Trilho mantém nomes acessíveis.
- **Selo de fonte:** cápsula Inter (10px, peso 550), fundo vermelho translúcido, borda `rgb(255 77 77 / .25)`; seletor de fonte usa estado `aria-pressed` e indisponibilidade explícita.
- **Resultados e descoberta:** containers arredondados; destaque usa gradiente `linear-gradient(115deg, rgb(81 118 153 / .32), rgb(31 44 66 / .65))`. Linhas têm play, favoritar, salvar e adicionar à fila; coração e marcador usam gelo com preenchimento translúcido no estado selecionado e `aria-pressed`. Hover usa raised. O destaque revela sua chegada por recorte (400ms, cubic-bezier(.16,1,.3,1)), de inset inferior (8%) ao recorte completo, com cantos (24px). Títulos longos truncam na linha, com texto completo no atributo title.
- **Inspector e player:** capa atual, fila removível e seleção real. Autoplay é opt-in com toggle acessível, prepara um candidato durante a reprodução e continua quando ativado após o fim da fila; entradas manuais têm precedência. Faixa atual usa gelo translúcido. Dock mantém play/pause, anterior/próxima, timeline e volume; stop desaparece até 899px. Avisos usam status/alert. SoundCloud público está disponível sem credenciais do usuário; a barra lateral identifica integração pública ou API oficial conforme a configuração.

- **Biblioteca:** salvas, favoritos, playlists e histórico de escuta qualificada persistem como metadados locais. Seletores de coleção e playlists usam cápsulas ardósia, contagens discretas e gelo translúcido com `aria-pressed`. Formulários rotulados criam e renomeiam playlists; exclusão e limpeza de histórico pedem confirmação inline. Estados vazios encaminham à busca.
- **Descobertas pessoais:** listas de recomendações usam surface, hover raised, capas compactas e motivo em gelo. Afinidade local por artista/título, favoritos e escutas orienta a seleção; as últimas 20 faixas ouvidas são excluídas, duplicatas são filtradas e há limite por artista para diversidade. Atualizar e tentar novamente são ações explícitas.
- **Atalhos:** botão na barra abre ajuda não modal com teclas em Inter sobre raised; oferece busca, transporte, seek, volume, favoritos, salvar, fila e navegação. Atalhos respeitam campos de edição e exigem a janela em foco. O painel mantém fechamento explícito e Escape.

Transições de fundo/cor/borda usam (160ms, ease-out). O link principal das ideias desloca sua seta (4px) no hover com transição (200ms). `prefers-reduced-motion` remove transições, o recorte de chegada e o deslocamento da seta, mantendo rolagem automática. A atmosfera só muda com a capa; não há movimento decorativo contínuo.

## Do's and Don'ts

### Do:
- Do preservar vidro azul e acento gelo nos estados ativos.
- Do manter foco visível, rótulos acessíveis e redução de movimento.
- Do preservar a timeline utilizável no player móvel e títulos completos acessíveis.

### Don't:
- Don't reintroduzir carvão/menta como identidade principal.
- Don't adicionar blur a cada linha ou animação contínua aos campos de cor.
- Don't ocultar estados de erro das fontes públicas ou prometer reprodução offline a partir dos metadados salvos.
