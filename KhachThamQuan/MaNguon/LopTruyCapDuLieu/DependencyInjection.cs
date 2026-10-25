using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisitorGuide.Application.Repositories;
using VisitorGuide.Infrastructure.Repositories;

namespace VisitorGuide.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("VisitorGuide")
            ?? throw new InvalidOperationException(
                "Connection string 'VisitorGuide' is missing. Configure ConnectionStrings:VisitorGuide.");

        services.AddDbContext<VisitorGuideDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDiaDiemThamQuanRepository, SqlServerDiaDiemThamQuanRepository>();
        return services;
    }
}
