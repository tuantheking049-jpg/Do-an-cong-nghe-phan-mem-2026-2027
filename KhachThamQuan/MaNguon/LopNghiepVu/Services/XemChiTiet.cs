namespace VisitorGuide.Application.Services;

public sealed class XemChiTiet(XemDiaDiem xemDiaDiem)
{
    public ChiTietDiaDiemDto? ThucHien(string id, string language)
    {
        var place = xemDiaDiem.ThucHien(id, language);
        return place is null ? null : new ChiTietDiaDiemDto(place.Name, place.Description);
    }
}

public sealed record ChiTietDiaDiemDto(string TenDiaDiem, string NoiDung);
