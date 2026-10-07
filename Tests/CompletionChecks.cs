using Microsoft.JSInterop;
using Voxen.Services;
using Voxen.Models;
using System.Text;
static class CompletionChecks
{
    public static async Task RunAsync()
    {
        foreach(var recover in new[]{true,false})
        {
            var source=new Source(recover);
            using var preparation=new AudioPreparationService(source);
            await using var proxy=new AudioProxy();await using var native=new NativeAudioService();
            await using var player=new PlayerService(preparation,proxy,native);
            var queue=new QueueService();using var playback=new PlaybackCoordinator(queue,player,new RecommendationService(source));
            await player.InitializeAsync(new NoJs());
            var track=new Track("first","First","Fixture","",TimeSpan.FromSeconds(8),TrackSource.YouTube,"https://youtube.com");
            await playback.PlayTrackAsync(track);
            playback.Add(track with {Id="next",Title="Next",Duration=TimeSpan.FromSeconds(1)});
            await Task.Delay(2200);
            Require(player.CurrentTrack?.Id=="first","Premature EOF cannot advance the queue");
            var deadline=DateTime.UtcNow.AddSeconds(12);
            while(player.CurrentTrack?.Id=="first" && player.Status!=PlaybackStatus.Error && DateTime.UtcNow<deadline)await Task.Delay(50);
            if(recover)Require(player.CurrentTrack?.Id=="next" && source.FirstCalls==2,"Interrupted audio resumes the same track and advances only after full completion");
            else
            {
                Require(player.CurrentTrack?.Id=="first" && player.Status==PlaybackStatus.Error && source.FirstCalls<=3,"Persistent truncation stops recovery without skipping or looping");
                var heard=player.Position;
                source.Recover=true;
                await player.ToggleAsync();
                Require(player.Position>=heard && heard>0,"Manual retry preserves the interrupted playback position");
                var retryDeadline=DateTime.UtcNow.AddSeconds(12);
                while(player.CurrentTrack?.Id=="first" && player.Status!=PlaybackStatus.Error && DateTime.UtcNow<retryDeadline)await Task.Delay(50);
                Require(player.CurrentTrack?.Id=="next","Manual continuation reaches completion before advancing");
            }
            await playback.StopAsync();
        }
        Require(!NativeAudioService.IsPrematureEnd(188,187.5) && NativeAudioService.IsPrematureEnd(188,90),"Completion tolerates codec rounding and detects significant truncation");
    }
    private static void Require(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
    private sealed class Source(bool recover):IAudioSourceProvider,ITrackSearchProvider
    {
        public int FirstCalls;
        public bool Recover=recover;
        public Task<IReadOnlyList<Track>> SearchAsync(string query,CancellationToken token=default)=>Task.FromResult<IReadOnlyList<Track>>([]);
        public Task<AudioResource> ResolveAsync(Track track,CancellationToken token)
        {
            var attempt=track.Id=="next" ? 0 : Interlocked.Increment(ref FirstCalls);
            var length=track.Id=="next" ? 1 : attempt>1 ? (Recover ? 8 : 3) : 1;
            using var memory=new MemoryStream();using(var writer=new BinaryWriter(memory,Encoding.UTF8,true)){
                var size=192000*length;writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(size+36);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)2);writer.Write(48000);writer.Write(192000);writer.Write((short)4);writer.Write((short)16);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(size);writer.Write(new byte[size]);
            }
            var bytes=memory.ToArray();return Task.FromResult(new AudioResource("audio/wav",_=>Task.FromResult<Stream>(new MemoryStream(bytes))));
        }
    }
    private sealed class NoJs:IJSRuntime { public ValueTask<T> InvokeAsync<T>(string id,object?[]? args)=>throw new Exception("Native only");public ValueTask<T> InvokeAsync<T>(string id,CancellationToken token,object?[]? args)=>InvokeAsync<T>(id,args); }
}
