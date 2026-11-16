using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorGuide.Infrastructure;

namespace VisitorGuide.Application.Tests;

public sealed class JsonPlaceFileStoreTests
{
    [Fact]
    public async Task InitializeAsync_LoadsAndPersistsPlacesThroughJsonFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"visitor-guide-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "places.json");

        try
        {
            using var firstProvider = CreateProvider(filePath);
            await DatabaseInitializer.InitializeAsync(firstProvider);

            await using (var scope = firstProvider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();
                var place = await dbContext.DiaDiem.SingleAsync(item => item.Id == "cho-noi-nga-nam");
                place.Name = "Chợ nổi đã chỉnh sửa";
                await dbContext.SaveChangesAsync();
            }

            Assert.True(File.Exists(filePath));
            Assert.Contains("Chợ nổi đã chỉnh sửa", await File.ReadAllTextAsync(filePath));

            using var secondProvider = CreateProvider(filePath);
            await DatabaseInitializer.InitializeAsync(secondProvider);

            await using var reloadedScope = secondProvider.CreateAsyncScope();
            var reloadedContext = reloadedScope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();
            var reloadedPlace = await reloadedContext.DiaDiem
                .Include(item => item.GalleryPhotos)
                .SingleAsync(item => item.Id == "cho-noi-nga-nam");

            Assert.Equal("Chợ nổi đã chỉnh sửa", reloadedPlace.Name);
            Assert.Equal(6, await reloadedContext.DiaDiem.CountAsync());
            Assert.NotEmpty(reloadedPlace.GalleryPhotos);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ServiceProvider CreateProvider(string filePath)
    {
        var services = new ServiceCollection();
        services.AddJsonFileDataAccess(filePath);
        return services.BuildServiceProvider();
    }
}
