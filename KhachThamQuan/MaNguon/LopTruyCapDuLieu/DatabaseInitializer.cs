using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorGuide.Infrastructure.Entities;
using VisitorGuide.Infrastructure.SeedData;

namespace VisitorGuide.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (await dbContext.DiaDiem.AnyAsync(cancellationToken))
        {
            return;
        }

        var places = NguonDuLieuKhoiTao.DocDiaDiem();
        dbContext.DiaDiem.AddRange(places.Select(place => new DiaDiemEntity
        {
            Id = place.Id,
            Name = place.Name,
            Category = place.Category,
            Province = place.Province,
            DurationMinutes = place.DurationMinutes,
            Rating = place.Rating,
            ReviewCount = place.ReviewCount,
            ImageUrl = place.ImageUrl,
            ImageSourceUrl = place.ImageSourceUrl,
            ImagePosition = place.ImagePosition,
            Descriptions = place.Descriptions.Select(description => new DiaDiemMoTaEntity
            {
                LanguageCode = description.Key,
                Description = description.Value
            }).ToList(),
            Highlights = place.Highlights.Select((highlight, position) => new DiaDiemDiemNoiBatEntity
            {
                Position = position,
                Text = highlight
            }).ToList(),
            GalleryPhotos = place.GalleryPhotos.Select((photo, position) => new DiaDiemAnhEntity
            {
                Position = position,
                Url = photo.Url,
                SourceUrl = photo.SourceUrl,
                Caption = photo.Caption
            }).ToList()
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
