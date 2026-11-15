using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VisitorGuide.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();

        try
        {
            _ = await dbContext.Pois.AsNoTracking().AnyAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Không đọc được bảng POIs trong database DuLichSocTrang. " +
                "Hãy chạy Database/database_v2.sql trong SSMS trước, rồi kiểm tra ConnectionStrings:VisitorGuide.",
                exception);
        }
    }
}
