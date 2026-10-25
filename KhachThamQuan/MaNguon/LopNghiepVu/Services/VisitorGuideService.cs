namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;

public sealed class VisitorGuideService : IVisitorGuideService
{
    private readonly TimDiaDiem _timDiaDiem;
    private readonly XemDiaDiem _xemDiaDiem;

    public VisitorGuideService(IDiaDiemThamQuanRepository repository)
    {
        var chonNgonNgu = new ChonNgonNgu();
        _timDiaDiem = new TimDiaDiem(repository, chonNgonNgu);
        _xemDiaDiem = new XemDiaDiem(repository, chonNgonNgu);
    }

    public IReadOnlyList<DiaDiemThamQuanDto> GetDanhSachDiaDiem(
        string? search,
        string? category,
        string language) => _timDiaDiem.ThucHien(search, category, language);

    public DiaDiemThamQuanDto? GetDiaDiem(string id, string language) =>
        _xemDiaDiem.ThucHien(id, language);
}
