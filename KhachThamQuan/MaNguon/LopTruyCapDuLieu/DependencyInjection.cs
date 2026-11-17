using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorGuide.Application.Repositories;
using VisitorGuide.Infrastructure.Repositories;

namespace VisitorGuide.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddJsonFileDataAccess(
        this IServiceCollection services,
        string placesFilePath)
    {
        var databaseName = $"VisitorGuide-{Guid.NewGuid():N}";
        services.AddSingleton(new JsonPlaceFileStore(placesFilePath));
        services.AddDbContext<VisitorGuideDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IDiaDiemThamQuanRepository, EntityFrameworkDiaDiemThamQuanRepository>();
        return services;
    }
}
