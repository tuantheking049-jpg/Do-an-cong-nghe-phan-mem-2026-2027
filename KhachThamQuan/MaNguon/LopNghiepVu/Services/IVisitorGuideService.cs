namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;

public interface IVisitorGuideService
{
    IReadOnlyList<DiaDiemThamQuanDto> GetDanhSachDiaDiem(string? search, string? category, string language);

    DiaDiemThamQuanDto? GetDiaDiem(string id, string language);
}
