using VisitorGuide.Application;
using VisitorGuide.Application.Repositories;
using VisitorGuide.Infrastructure.SeedData;

namespace VisitorGuide.Infrastructure.Repositories;

public sealed class KhoDiaDiemThamQuanTrongBoNho : IDiaDiemThamQuanRepository
{
    private readonly IReadOnlyList<DiaDiemThamQuan> _places = NguonDuLieuKhoiTao.DocDiaDiem();

    public IReadOnlyCollection<DiaDiemThamQuan> GetAll() => _places;

    public DiaDiemThamQuan? GetById(string id) =>
        _places.FirstOrDefault(place => place.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
