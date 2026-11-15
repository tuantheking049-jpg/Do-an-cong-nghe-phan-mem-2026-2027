using System.Text.Json;

namespace VisitorGuide.Infrastructure;

public sealed record PhotoJson(string Url, string SourceUrl, string Caption);

public static class PoiJson
{
    public const string EmptyNarrationPlaceholder = "(Chưa có nội dung thuyết minh)";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static List<string> ReadHighlights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string WriteHighlights(IEnumerable<string> highlights) =>
        JsonSerializer.Serialize(highlights.ToList(), Options);

    public static List<PhotoJson> ReadGallery(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<PhotoJson>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string WriteGallery(IEnumerable<PhotoJson> photos) =>
        JsonSerializer.Serialize(photos.ToList(), Options);
}
