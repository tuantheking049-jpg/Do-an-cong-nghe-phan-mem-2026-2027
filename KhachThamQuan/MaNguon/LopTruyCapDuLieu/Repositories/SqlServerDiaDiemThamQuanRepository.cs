using Microsoft.EntityFrameworkCore;
using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure.Repositories;

public sealed class SqlServerDiaDiemThamQuanRepository(VisitorGuideDbContext dbContext)
    : IDiaDiemThamQuanRepository
{
    public IReadOnlyCollection<DiaDiemThamQuan> GetAll() =>
        dbContext.DiaDiem
            .AsNoTracking()
            .Include(place => place.Descriptions)
            .Include(place => place.Highlights)
            .Include(place => place.GalleryPhotos)
            .AsSplitQuery()
            .ToArray()
            .Select(ToDomain)
            .ToArray();

    public DiaDiemThamQuan? GetById(string id)
    {
        var place = dbContext.DiaDiem
            .AsNoTracking()
            .Include(item => item.Descriptions)
            .Include(item => item.Highlights)
            .Include(item => item.GalleryPhotos)
            .AsSplitQuery()
            .SingleOrDefault(item => item.Id == id);

        return place is null ? null : ToDomain(place);
    }

    private static DiaDiemThamQuan ToDomain(DiaDiemEntity place) => new(
        place.Id,
        place.Name,
        place.Category,
        place.Province,
        place.DurationMinutes,
        place.Rating,
        place.ReviewCount,
        place.ImageUrl,
        place.ImageSourceUrl,
        place.ImagePosition,
        place.Highlights.OrderBy(item => item.Position).Select(item => item.Text).ToArray(),
        place.Descriptions.ToDictionary(item => item.LanguageCode, item => item.Description, StringComparer.OrdinalIgnoreCase),
        place.GalleryPhotos.OrderBy(item => item.Position)
            .Select(item => new DiaDiemPhoto(item.Url, item.SourceUrl, item.Caption))
            .ToArray());
}
