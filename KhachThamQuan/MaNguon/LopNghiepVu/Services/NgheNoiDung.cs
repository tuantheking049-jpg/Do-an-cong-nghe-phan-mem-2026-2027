namespace VisitorGuide.Application.Services;

public sealed class NgheNoiDung(
    XemDiaDiem xemDiaDiem,
    ChonNgonNgu chonNgonNgu)
{
    public NoiDungThuyetMinhDto? ThucHien(string id, string language)
    {
        var selectedLanguage = chonNgonNgu.ThucHien(language);
        var place = xemDiaDiem.ThucHien(id, selectedLanguage.Ma);
        return place is null
            ? null
            : new NoiDungThuyetMinhDto(place, selectedLanguage.MaGiongDoc);
    }
}

public sealed record NoiDungThuyetMinhDto(
    VisitorGuide.Application.DiaDiemThamQuanDto DiaDiem,
    string MaGiongDoc);
