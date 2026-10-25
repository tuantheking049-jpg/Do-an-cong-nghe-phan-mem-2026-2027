namespace VisitorGuide.Application.Repositories;

using VisitorGuide.Application;

public interface IDiaDiemThamQuanRepository
{
    IReadOnlyCollection<DiaDiemThamQuan> GetAll();

    DiaDiemThamQuan? GetById(string id);
}
