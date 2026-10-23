namespace VisitorGuide.Application;

public sealed record Landmark(
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
    IReadOnlyDictionary<string, string> Descriptions);

public sealed record LandmarkDto(
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
    IReadOnlyList<string> Languages);
