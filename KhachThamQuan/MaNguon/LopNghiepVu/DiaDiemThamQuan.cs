namespace VisitorGuide.Application;

public sealed record DiaDiemThamQuan(
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
    IReadOnlyList<string> Highlights,
    IReadOnlyDictionary<string, string> Descriptions,
    IReadOnlyList<DiaDiemPhoto> GalleryPhotos);

public sealed record DiaDiemThamQuanDto(
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
    IReadOnlyList<string> Highlights,
    string Description,
    IReadOnlyList<string> Languages,
    IReadOnlyList<DiaDiemPhoto> GalleryPhotos);

public sealed record DiaDiemPhoto(string Url, string SourceUrl, string Caption);
