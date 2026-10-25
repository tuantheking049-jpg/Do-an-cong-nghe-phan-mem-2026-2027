namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;

internal static class DiaDiemThamQuanDtoFactory
{
    public static DiaDiemThamQuanDto Tao(DiaDiemThamQuan diaDiem, string language)
    {
        if (!diaDiem.Descriptions.TryGetValue(language, out var description))
        {
            throw new InvalidOperationException(
                $"Thiếu bản dịch '{language}' cho địa danh '{diaDiem.Id}'.");
        }

        return new DiaDiemThamQuanDto(
            diaDiem.Id,
            diaDiem.Name,
            diaDiem.Category,
            diaDiem.Province,
            diaDiem.DurationMinutes,
            diaDiem.Rating,
            diaDiem.ReviewCount,
            diaDiem.ImageUrl,
            diaDiem.ImageSourceUrl,
            diaDiem.ImagePosition,
            diaDiem.Highlights,
            description,
            ["vi", "en", "fr"],
            [
                new DiaDiemPhoto(diaDiem.ImageUrl, diaDiem.ImageSourceUrl, diaDiem.Name),
                .. diaDiem.GalleryPhotos
            ]);
    }
}
