namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;

public interface IVisitorGuideService
{
    IReadOnlyList<LandmarkDto> GetLandmarks(string? search, string? category, string language);

    LandmarkDto? GetLandmark(string id, string language);
}
