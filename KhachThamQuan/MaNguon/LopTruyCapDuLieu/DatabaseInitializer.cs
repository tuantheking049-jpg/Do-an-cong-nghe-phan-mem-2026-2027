using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VisitorGuide.Infrastructure;

public static class DatabaseInitializer
{
    private const string TenScript = "VisitorGuide.Infrastructure.Database.database_v2.sql";
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();

        if (await CoTheDocBangAsync(dbContext, cancellationToken))
        {
            return;
        }

        try
        {
            await ChayScriptKhoiTaoAsync(dbContext.Database.GetConnectionString()!, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Không kết nối được SQL Server để tạo database DuLichSocTrang. " +
                "Hãy kiểm tra SQL Server đang chạy và ConnectionStrings:VisitorGuide (Server=...).",
                exception);
        }

        if (!await CoTheDocBangAsync(dbContext, cancellationToken))
        {
            throw new InvalidOperationException(
                "Đã chạy script tạo database nhưng vẫn không đọc được bảng POIs.");
        }
    }

    private static async Task<bool> CoTheDocBangAsync(VisitorGuideDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            _ = await dbContext.Pois.AsNoTracking().AnyAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task ChayScriptKhoiTaoAsync(string connectionString, CancellationToken cancellationToken)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TenScript)
            ?? throw new InvalidOperationException("Không tìm thấy script database_v2.sql được nhúng trong ứng dụng.");
        using var reader = new StreamReader(stream);
        var script = (await reader.ReadToEndAsync(cancellationToken)).TrimStart('\uFEFF');
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var cacLenh = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var lenh in cacLenh)
        {
            if (string.IsNullOrWhiteSpace(lenh))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = lenh;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
