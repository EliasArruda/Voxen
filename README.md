# Voxen

Player desktop em C# / .NET 10 / Blazor / PhotinoX.Blazor 5.3.3. Usa a WebView nativa do sistema, sem Electron ou Chromium embarcado. `Routes.razor` é o componente raiz; o Router usa `NotFoundPage` do .NET 10.

## Funcionalidades

- Busca YouTube com debounce de 350 ms, cancelamento, timeout, até 20 resultados e proteção contra respostas antigas.
- Play, pausa, retomada, parada, seek, volume, duração e posição reais. Áudio AAC obtido por YoutubeExplode, decodificado por FFmpeg e reproduzido em SDL3.
- Fila em memória, entradas independentes mesmo para faixas repetidas, remoção, limpeza, anterior e próxima. Fim da faixa avança automaticamente.
- Autoplay opcional: pesquisa recomendações nos últimos 15 segundos da última faixa. Prefetch não modifica a fila manual; desligar ou parar descarta a recomendação. Limite de 200 entradas.
- SoundCloud opcional com OAuth, cache/refresh de token, pesquisa de faixas públicas reproduzíveis e resolução HLS. Credenciais ausentes deixam a fonte desativada.
- Interface Studio com vidro translúcido azul, controles arredondados, fontes locais, foco de teclado e estados de erro/vazio. Painel de fila vira drawer abaixo de 1280px.

Biblioteca, favoritos e playlists persistidas ainda não estão implementados. Busca e recomendação do YouTube usam o canal como artista. Faixas bloqueadas, restritas ou sem AAC compatível podem falhar; a interface permite tentar novamente ou escolher outra.

## Executar

Requer SDK .NET 10, sessão gráfica e dependências nativas da PhotinoX. Linux usa WebKitGTK; Windows usa WebView2 instalado no sistema. O áudio usa FFmpeg + SDL3 e não depende dos codecs ou plugins GStreamer do WebView.

```sh
dotnet restore
dotnet run
```

Para desenvolver com `dotnet run`, tenha `ffmpeg` no PATH ou configure `VOXEN_FFMPEG_PATH` com seu caminho. Seu computador já possui FFmpeg. O runtime SDL3 acompanha o NuGet.

Para distribuir sem exigir SDK .NET nem instalação manual de FFmpeg:

```sh
python3 scripts/publish.py --rid linux-x64 --output artifacts/Voxen-linux-x64
python3 scripts/publish.py --rid win-x64 --output artifacts/Voxen-win-x64
```

O script publica .NET self-contained, inclui SDL3 e empacota FFmpeg 9.0 de um build fixado com SHA-256 verificado. O primeiro publish baixa um arquivo grande de build; ele fica em cache ignorado pelo Git. Os pacotes continuam exigindo a WebView nativa e um sistema de áudio funcional. Não existe uma dependência multimídia instalada em todos os computadores; distribuir o motor junto resolve a instalação separada de codecs para o usuário.

Não é necessário build de Node para executar. CSS e fontes são locais; arquivos Tailwind do projeto inicial foram preservados. FFmpeg reproduz HLS no desktop. Um backend HTML5 de testes do navegador mantém hls.js 1.7.3 como fallback opcional. Licenças acompanham fontes e biblioteca.

## SoundCloud

Configure localmente, antes de iniciar, `VOXEN_SOUNDCLOUD_CLIENT_ID` e `VOXEN_SOUNDCLOUD_CLIENT_SECRET`. Não coloque valores no código ou em commits; `.env` não é carregado automaticamente. Reinicie o aplicativo após configurar o ambiente. Não distribua suas credenciais em um binário.

Cada usuário precisa de aplicativo autorizado na API oficial. Sem credenciais, o YouTube continua funcionando. OAuth e metadados foram testados com HTTP controlado; **SoundCloud real ainda não foi validado**, porque não há credenciais disponíveis.

O stream HLS usa manifesto autenticado no backend e URLs assinadas de segmentos no WebView. Os tokens OAuth nunca entram no JavaScript. Restrições e permissões da fonte continuam aplicáveis.

## Arquitetura

`Blazor UI → serviços → providers`. `Track` não depende de YoutubeExplode. `MusicProviders` seleciona YouTube/SoundCloud; `SearchSession` pertence à página. `QueueService`, `PlayerService` e `RecommendationService` são separados; `PlaybackCoordinator` coordena seus eventos.

O desktop usa um processo FFmpeg e um stream SDL3. O pipe PCM e a saída mantêm cerca de um segundo de buffer; pausa, seek e volume são controlados pelo serviço. Eventos chegam à UI pelo dispatcher Blazor. A prévia de navegador usa o backend HTML5 para testar a mesma superfície. `AudioProxy` abre uma porta efêmera em `127.0.0.1` e entrega o stream atual com token de sessão e HTTP Range. Não grava músicas, não mantém um catálogo completo em memória e não expõe uma porta na rede local. A fila é descartada ao fechar o aplicativo.

## Verificar

```sh
dotnet build --configuration Release
dotnet run --project Tests/Voxen.Checks.csproj
node Tests/audio.checks.mjs
SDL_AUDIODRIVER=dummy dotnet run --project Tests/Voxen.Checks.csproj -- --native
```

Os checks cobrem pesquisa, fila, streaming Range, callbacks antigos, erros/retry, autoplay, parada durante recomendação, prioridade de adições manuais e contrato OAuth/HLS SoundCloud. Node é usado somente no teste de regressão JavaScript.

Busca externa opcional:

```sh
dotnet run --project Tests/Voxen.Checks.csproj -- --youtube
```

A CI executa build e checks determinísticos em Linux e Windows. Testes externos não rodam na CI. Validação local: áudio real do YouTube e fluxos da UI em Chromium usando os componentes compartilhados; reprodução real no Photino/Linux com FFmpeg + SDL3 (play, pausa, seek, retomada e parada). Execução gráfica no Windows exige validação adicional.

## Referências

- [PhotinoX](https://github.com/PhotinoX/PhotinoX)
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
- [SoundCloud: guia oficial](https://developers.soundcloud.com/docs/api/guide)
- [FFmpeg](https://ffmpeg.org/)
- [SDL3](https://libsdl.org/)
- [hls.js](https://github.com/video-dev/hls.js)

Direção visual: WaveMix Universal Search & Studio Player, tela Stitch `55b8670e6bac469f9037e3f83d6321a5`, adaptada à preferência de vidro e arredondamento do usuário. Os tokens ficam em `wwwroot/Styles/voxen.css` e `DESIGN.md`.
