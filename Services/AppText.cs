using System.Globalization;
using System.Text.Json;
namespace Voxen.Services;

public sealed class AppText(AppPreferences preferences)
{
    private static readonly Dictionary<string,Dictionary<string,string>> Resources = Load();
    private static Dictionary<string,Dictionary<string,string>> Load()
    {
        var assembly=typeof(AppText).Assembly;
        return new[]{"en","es"}.ToDictionary(lang=>lang,lang=>{
            using var stream=assembly.GetManifestResourceStream("Voxen.Resources."+lang+".json")!;
            return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
        });
    }
    public string this[string? text]
    {
        get
        {
            if(text is null)return "";
            if(!Resources.TryGetValue(preferences.Data.Language,out var values))return text;
            if(values.TryGetValue(text,out var translation))return translation;
            const string unavailable=" indisponível no momento. Os resultados da outra fonte continuam disponíveis.";
            if(text.EndsWith(unavailable))return Format("{0}"+unavailable,text[..^unavailable.Length]);
            if(text.StartsWith("A partir de "))return Format("A partir de {0}",text[12..]);
            return text;
        }
    }
    public string Format(string text,params object[] args)=>string.Format(CultureInfo.GetCultureInfo(preferences.Data.Language),this[text],args);
}
