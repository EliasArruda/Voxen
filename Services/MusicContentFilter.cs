using System.Text.Json;
using System.Text.RegularExpressions;
namespace Voxen.Services;
public static partial class MusicContentFilter
{
    [GeneratedRegex(@"\b(podcast|interview|entrevista|audiobook|audiolivro|spoken[ -]word|tutorial|gameplay|vlog|documentary|documentário)\b",RegexOptions.IgnoreCase)]
    private static partial Regex SpokenContent();
    public static bool Allows(JsonElement track)
    {
        var text=string.Join(" ",new[]{"genre","title","tag_list"}.Select(key=>track.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():""));
        return !SpokenContent().IsMatch(text);
    }
}
