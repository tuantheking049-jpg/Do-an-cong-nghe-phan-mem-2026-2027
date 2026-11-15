using Microsoft.EntityFrameworkCore;
using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure.Repositories;

public sealed class SqlServerDiaDiemThamQuanRepository(VisitorGuideDbContext dbContext)
    : IDiaDiemThamQuanRepository
{
    public IReadOnlyCollection<DiaDiemThamQuan> GetAll() =>
        dbContext.Pois
            .AsNoTracking()
            .Include(poi => poi.Narration)
                .ThenInclude(narration => narration!.Translations)
            .AsSplitQuery()
            .OrderBy(poi => poi.DisplayOrder)
            .ToArray()
            .Select(ToDomain)
            .ToArray();

    public DiaDiemThamQuan? GetById(string id)
    {
        var poi = dbContext.Pois
            .AsNoTracking()
            .Include(item => item.Narration)
                .ThenInclude(narration => narration!.Translations)
            .AsSplitQuery()
            .SingleOrDefault(item => item.Slug == id);

        return poi is null ? null : ToDomain(poi);
    }

    private static DiaDiemThamQuan ToDomain(PoiEntity poi)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var narration = poi.Narration;
        if (narration is not null)
        {
            descriptions[narration.SourceLang] = string.IsNullOrWhiteSpace(narration.Content)
                ? PoiJson.EmptyNarrationPlaceholder
                : narration.Content;

            foreach (var translation in narration.Translations)
            {
                descriptions[translation.LangCode] = translation.TranslatedContent;
            }
        }

        if (!descriptions.ContainsKey("vi"))
        {
            descriptions["vi"] = PoiJson.EmptyNarrationPlaceholder;
        }

        return new DiaDiemThamQuan(
            poi.Slug,
            poi.Name,
            poi.Category ?? "",
            poi.Province ?? "",
            poi.DurationMinutes,
            poi.Rating,
            poi.ReviewCount,
            poi.ImageUrl ?? "",
            poi.ImageSourceUrl ?? "",
            poi.ImagePosition ?? "center center",
            PoiJson.ReadHighlights(poi.HighlightsJson),
            descriptions,
            PoiJson.ReadGallery(poi.GalleryJson)
                .Select(photo => new DiaDiemPhoto(photo.Url, photo.SourceUrl, photo.Caption))
                .ToArray());
    }
}
