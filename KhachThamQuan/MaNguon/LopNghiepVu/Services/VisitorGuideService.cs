namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;

public sealed class VisitorGuideService(ILandmarkRepository repository) : IVisitorGuideService
{
    private static readonly string[] SupportedLanguages = ["vi", "en", "fr"];

    public IReadOnlyList<LandmarkDto> GetLandmarks(string? search, string? category, string language)
    {
        var query = search?.Trim();
        var landmarks = repository.GetAll()
            .Where(landmark => string.IsNullOrWhiteSpace(category)
                || landmark.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(landmark => string.IsNullOrWhiteSpace(query)
                || Matches(landmark, query))
            .Select(landmark => ToDto(landmark, language))
            .ToArray();

        return landmarks;
    }

    public LandmarkDto? GetLandmark(string id, string language)
    {
        var landmark = repository.GetById(id);
        return landmark is null ? null : ToDto(landmark, language);
    }

    private static bool Matches(Landmark landmark, string query)
    {
        return landmark.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || landmark.Province.Contains(query, StringComparison.OrdinalIgnoreCase)
            || landmark.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || landmark.Highlights.Any(highlight => highlight.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static LandmarkDto ToDto(Landmark landmark, string language)
    {
        if (!SupportedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Ngôn ngữ không được hỗ trợ.", nameof(language));
        }

        var normalizedLanguage = language.ToLowerInvariant();
        if (!landmark.Descriptions.TryGetValue(normalizedLanguage, out var description))
        {
            throw new InvalidOperationException(
                $"Thiếu bản dịch '{normalizedLanguage}' cho địa danh '{landmark.Id}'.");
        }

        return new LandmarkDto(
            landmark.Id,
            landmark.Name,
            landmark.Category,
            landmark.Province,
            landmark.DurationMinutes,
            landmark.Rating,
            landmark.ReviewCount,
            landmark.ImageUrl,
            landmark.ImageSourceUrl,
            landmark.ImagePosition,
            landmark.Highlights,
            description,
            SupportedLanguages);
    }
}
