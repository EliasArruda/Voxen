using System.Buffers.Binary;
using System.Text.Json;
using Voxen.Services;
internal static class PersonalizationChecks
{
    private static void Check(bool value,string message) { if(!value)throw new Exception(message);Console.WriteLine("PASS "+message); }
    public static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"voxen-settings-"+Guid.NewGuid());var path=Path.Combine(dir,"settings.json");
        try
        {
            using var preferences=new AppPreferences(path);var text=new AppText(preferences);
            Check(text["Buscar"]=="Buscar","Default UI language remains Portuguese");
            preferences.Language("en");preferences.Preset("bass");preferences.CoverColors(false);
            var saved=new AppPreferences(path);
            Check(saved.Data.Language=="en" && saved.Tone.Bass==8 && !saved.Data.CoverColors,"Language, tone and cover preference survive restart");
            Check(text["Configurações"]=="Settings" && text.Format("Reproduzir {0}","Title <unsafe>")=="Play Title <unsafe>","English resources preserve user metadata in format arguments");
            preferences.Language("es");Check(text["Biblioteca"]=="Biblioteca" && text["Pausar"]=="Pausar" && text["Sua música"]=="Tu música","Spanish UI resources are available");
            preferences.Equalizer(new(double.NaN,100,double.NegativeInfinity));Check(preferences.Tone==new AudioTone(0,12,0),"Invalid equalizer values are bounded");
            preferences.Language("invalid");Check(preferences.Data.Language=="pt-BR","Unsupported language falls back safely");
            File.WriteAllText(path,"{bad");Check(new AppPreferences(path).Error is not null,"Malformed settings report a recoverable error");
        }
        finally { if(Directory.Exists(dir))Directory.Delete(dir,true); }
        var pcm=Sine(100);var original=pcm.ToArray();new AudioToneProcessor(new()).Process(pcm);Check(pcm.SequenceEqual(original),"Flat tone leaves PCM samples bit exact");
        var low=Gain(40,new(8,0,0));var mid=Gain(1000,new(8,0,0));Check(low/mid>1.8,"Bass preset boosts low frequencies relative to midrange");
        var voice=Gain(1000,new(0,8,0));var outside=Gain(100,new(0,8,0));Check(voice/outside>1.8,"Midrange control targets the voice band");
        Check(Gain(12000,new(0,0,8))/Gain(1000,new(0,0,8))>1.7,"Treble control shapes high frequencies");
        var stereo=Sine(100);for(var i=2;i<stereo.Length;i+=4)BinaryPrimitives.WriteInt16LittleEndian(stereo.AsSpan(i),0);new AudioToneProcessor(new(12,12,12)).Process(stereo);Check(Enumerable.Range(0,stereo.Length/4).All(i=>BinaryPrimitives.ReadInt16LittleEndian(stereo.AsSpan(i*4+2))==0),"Equalizer channels stay independent with bounded samples");
        Check(CoverPaletteService.Select([240,20,20,230,30,30,2,2,2,255,255,255]) is { R:>200,G:<50 },"Cover color extraction favors the visible color over black/white margins");
        using var spoken=JsonDocument.Parse("{\"genre\":\"Podcast\",\"title\":\"Episode 12\"}");using var song=JsonDocument.Parse("{\"genre\":\"Rock\",\"title\":\"One More Time\"}");
        Check(!MusicContentFilter.Allows(spoken.RootElement)&&MusicContentFilter.Allows(song.RootElement),"SoundCloud excludes explicit spoken-content metadata");
        using var music=JsonDocument.Parse("""
        {"contents":[{"musicShelfRenderer":{"title":{"runs":[{"text":"Songs"}]},"contents":[{"musicResponsiveListItemRenderer":{"flexColumns":[{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Song name"}]}}},{"musicResponsiveListItemFlexColumnRenderer":{"text":{"runs":[{"text":"Artist name","navigationEndpoint":{"browseEndpoint":{"browseId":"UC123"}}},{"text":" • "},{"text":"3:42"}]}}}],"navigationEndpoint":{"watchEndpoint":{"videoId":"dQw4w9WgXcQ"}},"thumbnail":{"thumbnails":[{"url":"https://i.ytimg.com/example.jpg"}]}}}]}},{"musicShelfRenderer":{"title":{"runs":[{"text":"Videos"}]},"contents":[{"musicResponsiveListItemRenderer":{"navigationEndpoint":{"watchEndpoint":{"videoId":"abcdefghijk"}}}}]}}]}
        """);
        using var untitled=JsonDocument.Parse(music.RootElement.GetRawText().Replace("\"title\":{\"runs\":[{\"text\":\"Songs\"}]},", ""));
        Check(YouTubeMusicSearchService.Parse(untitled.RootElement).Count==1,"Filtered song shelves without category titles retain song results");
        var tracks=YouTubeMusicSearchService.Parse(music.RootElement);Check(tracks.Count==1 && tracks[0].Title=="Song name" && tracks[0].Artist=="Artist name" && tracks[0].Duration==TimeSpan.FromSeconds(222),"Song search maps metadata and rejects shelves outside Songs");
    }
    private static byte[] Sine(double frequency)
    {
        var pcm=new byte[48000*4];for(var i=0;i<48000;i++){var sample=(short)(Math.Sin(2*Math.PI*frequency*i/48000)*16000);BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i*4),sample);BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i*4+2),sample);}return pcm;
    }
    private static double Gain(double frequency,AudioTone tone)
    {
        var pcm=Sine(frequency);new AudioToneProcessor(tone).Process(pcm);double sum=0;
        for(var i=24000;i<48000;i++){var sample=BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i*4));sum+=(double)sample*sample;}
        return Math.Sqrt(sum/24000);
    }
}
