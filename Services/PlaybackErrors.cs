using System.Net;
using Voxen.Models;
using YoutubeExplode.Exceptions;
namespace Voxen.Services;

internal static class PlaybackErrors
{
    // Messages are categories, never raw provider exceptions containing signed URLs.
    public static string Describe(Exception error, TrackSource source) => error switch
    {
        OperationCanceledException or TimeoutException => "A fonte demorou para responder. Tente novamente ou confira sua conexão.",
        VideoUnavailableException => "Este vídeo está indisponível ou exige acesso no YouTube. Escolha outra faixa.",
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized } => "A fonte recusou o acesso ao áudio. Tente outra faixa ou abra o link original.",
        HttpRequestException { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.Gone } => "Esta faixa não está mais disponível na fonte original.",
        HttpRequestException => "Não foi possível conectar à fonte de áudio. Confira sua conexão e tente novamente.",
        YoutubeExplodeException => "O YouTube não forneceu um áudio compatível. Tente novamente ou escolha outra faixa.",
        _ when source == TrackSource.SoundCloud => "O SoundCloud não disponibilizou o áudio completo desta faixa. Tente outra ou abra o link original.",
        _ => "Não foi possível obter o áudio desta faixa. Tente novamente ou abra o link original."
    };
}
