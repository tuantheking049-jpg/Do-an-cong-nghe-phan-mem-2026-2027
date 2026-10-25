namespace VisitorGuide.Application.Services;

using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;

public sealed class XemDiaDiem(
    IDiaDiemThamQuanRepository repository,
    ChonNgonNgu chonNgonNgu)
{
    public DiaDiemThamQuanDto? ThucHien(string id, string language)
    {
        var selectedLanguage = chonNgonNgu.ThucHien(language).Ma;
        var place = repository.GetById(id);
        return place is null ? null : DiaDiemThamQuanDtoFactory.Tao(place, selectedLanguage);
    }
}
