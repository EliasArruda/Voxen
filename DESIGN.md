---
name: Voxen — WaveMix Studio
description: Estúdio musical escuro, compacto e orientado à busca.
colors:
  bg: "#080a0d"
  surface: "#101419"
  raised: "#171c22"
  hover: "#1f252e"
  ink: "#f5f7f8"
  muted: "#9aa4ae"
  primary: "#66f2b1"
  line: "#252b33"
  youtube: "#ff6666"
  primary-hover: "#94f6ca"
typography:
  headline:
    fontFamily: 'Geist, Segoe UI, Noto Sans, sans-serif'
    fontSize: '24px'
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
  small: '5px'
  control: '8px'
  container: '12px'
  symbol: '16px'
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
    rounded: '{rounded.control}'
    padding: '10px 16px'
  button-primary-hover:
    backgroundColor: '{colors.primary-hover}'
  button-secondary:
    backgroundColor: '{colors.raised}'
    textColor: '{colors.ink}'
    rounded: '{rounded.control}'
    padding: '10px 16px'
  search-field:
    backgroundColor: '{colors.surface}'
    textColor: '{colors.ink}'
    rounded: '{rounded.control}'
    padding: '0 12px'
  track-table:
    backgroundColor: '{colors.surface}'
    rounded: '{rounded.container}'
  nav-active:
    backgroundColor: '{colors.raised}'
    textColor: '{colors.primary}'
    rounded: '{rounded.control}'
    padding: '10px 13px'
  source-badge:
    backgroundColor: 'rgb(255 0 0 / .09)'
    textColor: '{colors.youtube}'
    rounded: '{rounded.pill}'
    padding: '2px 8px'
  result-highlight:
    backgroundColor: '{colors.raised}'
    rounded: '{rounded.container}'
    padding: '20px'
---

# Design System: Voxen

## Overview

**Creative North Star: "WaveMix Studio"**

WaveMix Studio organiza a descoberta musical como um estúdio escuro: superfícies carvão, listas compactas e menta para orientação e ações. A tipografia discreta mantém títulos, artistas e duração legíveis. O conteúdo musical conduz a composição; animações ficam restritas às mudanças de estado.

Direção escolhida pelo usuário: Stitch, projeto `14219647605047406845`, tela `55b8670e6bac469f9037e3f83d6321a5` (Universal Search & Studio Player). Este documento registra a implementação em `wwwroot/Styles/voxen.css`, `fonts.css` e componentes Blazor; a referência não substitui os valores implementados.

**Key Characteristics:**
- Superfícies escuras em camadas tonais.
- Busca e metadados com alta densidade legível.
- Navegação lateral e deck persistente.
- Estados vazios e controles indisponíveis honestos.

## Colors

### Primary

Menta (`primary`) identifica navegação ativa, foco, busca, indicadores disponíveis e ações principais. `primary-hover` ilumina a ação principal ao passar o ponteiro.

### Neutral

Carvão profundo (`bg`) sustenta o aplicativo; `surface` delimita barra, listas e deck; `raised` distingue destaque, capas vazias e seleção. `hover` indica interação. `ink` mantém o texto principal claro; `muted` identifica metadados; `line` separa regiões com bordas discretas.

YouTube (`youtube`) é uma cor de fonte musical, aplicada ao selo; não substitui o acento principal. A variante SoundCloud existe no CSS, mas o provedor ainda está indisponível.

## Typography

Geist é a voz principal; Inter organiza rótulos e números. Ambas são WOFF2 locais, com `font-display: swap` e licenças OFL. Os fallbacks seguem os tokens. Ícones são SVGs próprios.

Cabeçalhos de página usam `headline`; seções usam `title`. Títulos de faixa usam (12px, peso 550); artista usa (12px). Rótulos, duração e contagem usam Inter entre (10px) e (11px), com números tabulares nas durações. Parágrafos usam entrelinha (1.65) e limite (70ch). O destaque da busca usa título (23px, entrelinha 1.3), reduzido para (19px) e (16px). O destaque inicial usa (32px), reduzido para (28px).

## Layout

Janela nativa inicial (1280×800). Shell com altura `100dvh`, mínimo (450px), barra superior (56px), navegação (240px), inspector (280px) e deck (88px). Conteúdo principal rola independentemente, com largura máxima (1400px) e padding (28px 24px 40px).

Em larguras até (1279px), o inspector fica oculto e abre por botão como painel fixo. Até (899px), a navegação vira trilho de ícones (64px); artista passa para a linha abaixo do título, saída de áudio desaparece. Até (520px), trilho (52px), deck (82px), padding de conteúdo (24px 14px), descoberta em uma coluna; número e selo da faixa deixam a lista, preservando capa, título, artista e duração.

Lista desktop: grid `24px 38px minmax(0,2fr) minmax(0,1.1fr) 84px 46px`, gap (10px), padding horizontal (14px), cabeçalho (36px), faixa mínima (56px). O primeiro resultado também aparece em destaque; não representa seleção ou reprodução.

## Elevation & Depth

Camadas tonais e bordas de (1px) criam profundidade sem sombras nas superfícies principais. Apenas o inspector sobreposto recebe `-10px 0 28px rgb(0 0 0 / .35)`. Ordem: deck (10), inspector (20), atalho de conteúdo (30).

## Shapes

Controles e navegação usam `control`; tabelas, destaque e inspector art usam `container`. Capas pequenas usam `small`; símbolo vazio usa `symbol`. Selo de fonte é uma cápsula; botão de reprodução é circular. Títulos longos recebem truncamento; o destaque limita o título a duas linhas.

## Components

- **Botões:** ação principal menta com texto carvão; secundária em superfície elevada e borda. Altura mínima (42px), peso (650), fonte (12px). Hover muda fundo; foco global tem contorno menta (2px), offset (4px).
- **Busca:** superfície escura, borda discreta e foco na borda menta; input (42px), rótulo acessível e limpar com nome explícito. Busca automática a partir de dois caracteres, limite de 200.
- **Navegação:** itens mínimos (42px), ícones próprios, estado ativo com menta e borda esquerda (2px). No trilho compacto, títulos continuam disponíveis.
- **Selo de fonte:** Inter (10px, peso 550), fundo vermelho translúcido e borda `rgb(255 77 77 / .25)` para YouTube.
- **Lista e destaque:** miniaturas remotas ou SVG musical; metadados com ellipsis e duração tabular. Hover da linha usa superfície elevada, sem sugerir reprodução disponível.
- **Estados vazios:** painel tonal com título e explicação. Skeleton mantém altura da linha. Erros e carregamento seguem a mesma hierarquia legível.
- **Inspector e deck:** fila vazia e ausência de faixa são intencionais. Controles do deck permanecem desabilitados; biblioteca e SoundCloud mostram “Em breve”.

Transições implementadas: fundo/cor/borda (160ms, ease-out). `prefers-reduced-motion` remove transições e mantém rolagem automática.

## Do's and Don'ts

### Do:
- Do preservar a paleta carvão e o acento menta nos estados ativos.
- Do manter foco visível, rótulos acessíveis e redução de movimento.
- Do representar busca, carregamento, erro e ausência de resultados com texto claro.

### Don't:
- Don't apresentar fila, biblioteca ou reprodução como recursos disponíveis antes da implementação.
- Don't substituir os SVGs próprios por fontes de ícones ou dependências decorativas.
- Don't transformar miniaturas ou títulos longos em causas de overflow.
