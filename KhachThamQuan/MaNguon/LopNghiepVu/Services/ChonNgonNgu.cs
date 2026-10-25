namespace VisitorGuide.Application.Services;

public sealed class ChonNgonNgu
{
    private static readonly IReadOnlyDictionary<string, NgonNguThuyetMinh> SupportedLanguages =
        new Dictionary<string, NgonNguThuyetMinh>(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = new("vi", "Tiếng Việt", "vi-VN"),
            ["en"] = new("en", "English", "en-US"),
            ["fr"] = new("fr", "Français", "fr-FR")
        };

    public IReadOnlyList<string> DanhSachMaNgonNgu => SupportedLanguages.Keys.ToArray();

    public NgonNguThuyetMinh ThucHien(string language)
    {
        if (string.IsNullOrWhiteSpace(language)
            || !SupportedLanguages.TryGetValue(language.Trim(), out var selectedLanguage))
        {
            throw new ArgumentException("Ngôn ngữ không được hỗ trợ.", nameof(language));
        }

        return selectedLanguage;
    }
}

public sealed record NgonNguThuyetMinh(string Ma, string Ten, string MaGiongDoc);
