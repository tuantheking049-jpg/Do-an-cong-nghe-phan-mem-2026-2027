using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using VisitorGuide.Application;
using VisitorGuide.Infrastructure.Entities;
using VisitorGuide.Infrastructure.SeedData;

namespace VisitorGuide.Infrastructure;

public sealed class JsonPlaceFileStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task<List<DiaDiemEntity>> LoadEntitiesAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            await using var stream = File.OpenRead(filePath);
            var places = await JsonSerializer.DeserializeAsync<List<DiaDiemThamQuan>>(
                    stream,
                    JsonOptions,
                    cancellationToken)
                ?? throw new InvalidDataException($"File dữ liệu địa điểm không hợp lệ: {filePath}");
            return places.Select(ToEntity).ToList();
        }

        return NguonDuLieuKhoiTao.DocDiaDiem().Select(ToEntity).ToList();
    }

    public List<DiaDiemEntity> DeserializeEntities(string json)
    {
        var places = JsonSerializer.Deserialize<List<DiaDiemThamQuan>>(json, JsonOptions)
            ?? throw new InvalidDataException("Dữ liệu địa điểm trong SQL Server không hợp lệ.");
        return places.Select(ToEntity).ToList();
    }

    public async Task<string> SerializeEntitiesAsync(
        VisitorGuideDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.DiaDiem
            .AsNoTracking()
            .Include(place => place.Descriptions)
            .Include(place => place.Highlights)
            .Include(place => place.GalleryPhotos)
            .ToListAsync(cancellationToken);
        return JsonSerializer.Serialize(entities.Select(ToDomain).ToArray(), JsonOptions);
    }

    public async Task SaveAsync(VisitorGuideDbContext dbContext, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = await SerializeEntitiesAsync(dbContext, cancellationToken);
            var directory = Path.GetDirectoryName(filePath)
                ?? throw new InvalidOperationException($"Không xác định được thư mục dữ liệu: {filePath}");
            Directory.CreateDirectory(directory);

            var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
                File.Move(temporaryPath, filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static DiaDiemEntity ToEntity(DiaDiemThamQuan place) => new()
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
    };

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
        place.Descriptions.ToDictionary(
            item => item.LanguageCode,
            item => item.Description,
            StringComparer.OrdinalIgnoreCase),
        place.GalleryPhotos.OrderBy(item => item.Position)
            .Select(item => new DiaDiemPhoto(item.Url, item.SourceUrl, item.Caption))
            .ToArray());
}
