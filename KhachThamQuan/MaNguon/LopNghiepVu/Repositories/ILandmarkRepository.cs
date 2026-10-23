namespace VisitorGuide.Application.Repositories;

using VisitorGuide.Application;

public interface ILandmarkRepository
{
    IReadOnlyCollection<Landmark> GetAll();

    Landmark? GetById(string id);
}
