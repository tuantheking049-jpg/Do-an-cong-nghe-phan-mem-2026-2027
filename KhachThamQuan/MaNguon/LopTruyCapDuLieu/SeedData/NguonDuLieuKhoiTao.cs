using System.Reflection;
using System.Text.Json;
using VisitorGuide.Application;

namespace VisitorGuide.Infrastructure.SeedData;

public static class NguonDuLieuKhoiTao
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<DiaDiemThamQuan> DocDiaDiem()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("VisitorGuide.Infrastructure.SeedData.places.json")
            ?? throw new InvalidOperationException("Không tìm thấy dữ liệu khởi tạo địa điểm.");

        var places = JsonSerializer.Deserialize<List<DiaDiemSeed>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Dữ liệu khởi tạo địa điểm không hợp lệ.");

        return places.Select(place => new DiaDiemThamQuan(
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
            place.Highlights,
            place.Descriptions,
            place.GalleryPhotos.Select(photo =>
                new DiaDiemPhoto(photo.Url, photo.SourceUrl, photo.Caption)).ToArray()))
            .ToArray();
    }

    private sealed record DiaDiemSeed(
        string Id,
        string Name,
        string Category,
        string Province,
        int DurationMinutes,
        double Rating,
        int ReviewCount,
        string ImageUrl,
        string ImageSourceUrl,
        string ImagePosition,
        string[] Highlights,
        Dictionary<string, string> Descriptions,
        DiaDiemPhotoSeed[] GalleryPhotos);

    private sealed record DiaDiemPhotoSeed(string Url, string SourceUrl, string Caption);
}
