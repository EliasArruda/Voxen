# Voxen

Player desktop em C# / .NET 10 / Blazor / PhotinoX.Blazor 5.3.3.
Usa a WebView nativa do sistema. Não depende de `App.razor`.

## Incremento atual: etapas 1–4

- `Track` e `TrackSource`: metadados independentes da biblioteca, incluindo URL da origem.
- `YouTubeService`: integração exclusiva com YoutubeExplode; até 20 resultados por busca, sem requisições adicionais por vídeo.
- `SearchSession`: debounce de 350 ms, mínimo de 2 caracteres, cancelamento, timeout de 20 segundos e proteção contra respostas antigas.
- Páginas `/`, `/search` e `/not-found`; `Routes.razor` com `NotFoundPage` do .NET 10.
- Componentes pequenos para input e resultados; estados de espera, carregamento, erro, retry e vazio.
- Interface adaptada do WaveMix Studio (Stitch): mint, fontes locais Geist/Inter, ícones SVG, lista compacta, painel lateral e dock persistente.
- Janela inicial de 1280 × 800; painel de fila recolhido abaixo de 1280px e navegação por ícones abaixo de 900px.
- Sugestões na Home abrem buscas reais via `/search?q=...`. O primeiro resultado aparece em destaque.

A duração ausente aparece como “—”. O canal do YouTube é mostrado como artista; não há inferência de metadados musicais.
Fila e reprodução ainda não estão implementadas. O painel de fila está vazio e os controles do dock estão desabilitados, com indicação “Em breve”. `Track.Url` identifica o vídeo, não um stream de áudio.

## Executar

Requer SDK .NET 10 e as dependências nativas da PhotinoX para o sistema operacional.
No Linux, execute em uma sessão gráfica com WebKitGTK compatível com PhotinoX instalado.

```sh
dotnet restore
dotnet run
```

O estilo e as fontes atuais são locais, sem build de Node obrigatório. Licenças OFL estão em `wwwroot/Fonts`. A imagem decorativa da Home é uma referência remota do projeto Stitch e possui fallback de superfície; os resultados usam thumbnails do YouTube. Os arquivos Tailwind existentes foram preservados.

## Verificar

Checks determinísticos, sem framework adicional de testes:

```sh
dotnet build
dotnet run --project Tests/Voxen.Checks.csproj
```

Teste opcional com requests reais ao YouTube:

```sh
dotnet run --project Tests/Voxen.Checks.csproj -- --youtube
```

Os checks exercitam debounce, cancelamento em andamento, resposta antiga, consultas repetidas, input curto, falha, retry e descarte.
O teste externo pode falhar quando a conexão ou os endpoints do YouTube estiverem indisponíveis.

## Próximos incrementos

5. `QueueService` com anterior, atual e próximas.
6. Adicionar resultado à fila.
7–10. Player central, fim da faixa, avanço automático e controles.
11–12. Recomendação independente e autoplay.
13. Provider de pesquisa e reprodução SoundCloud.

`ITrackSearchProvider` é a fronteira reutilizável para pesquisa de outras fontes. A sessão é criada por uma factory e pertence à página; sair dela cancela o trabalho e libera os resultados.

## Referências

- [Router e NotFoundPage no .NET 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#blazor).
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode).

## Referência visual

Stitch: `projects/14219647605047406845`, tela `55b8670e6bac469f9037e3f83d6321a5` — WaveMix Universal Search & Studio Player.
A versão Studio foi escolhida explicitamente pelo usuário. Identidade da aplicação mantida como Voxen.
