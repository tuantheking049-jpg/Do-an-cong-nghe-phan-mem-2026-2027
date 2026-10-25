namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;

public sealed class TimDiaDiem(
    IDiaDiemThamQuanRepository repository,
    ChonNgonNgu chonNgonNgu)
{
    public IReadOnlyList<DiaDiemThamQuanDto> ThucHien(
        string? search,
        string? category,
        string language)
    {
        var selectedLanguage = chonNgonNgu.ThucHien(language).Ma;
        var query = search?.Trim();

        return repository.GetAll()
            .Where(place => string.IsNullOrWhiteSpace(category)
                || place.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(place => string.IsNullOrWhiteSpace(query) || Matches(place, query))
            .Select(place => DiaDiemThamQuanDtoFactory.Tao(place, selectedLanguage))
            .ToArray();
    }

    private static bool Matches(DiaDiemThamQuan place, string query)
    {
        return place.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || place.Province.Contains(query, StringComparison.OrdinalIgnoreCase)
            || place.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || place.Highlights.Any(highlight =>
                highlight.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
