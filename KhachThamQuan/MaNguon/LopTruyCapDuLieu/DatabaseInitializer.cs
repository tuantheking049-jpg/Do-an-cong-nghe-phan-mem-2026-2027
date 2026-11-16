using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VisitorGuide.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VisitorGuideDbContext>();
        var placeFileStore = scope.ServiceProvider.GetRequiredService<JsonPlaceFileStore>();
        var sqlServerApplicationStore = scope.ServiceProvider.GetService<SqlServerApplicationStore>();
        var relationalStore = scope.ServiceProvider.GetService<SqlServerRelationalStore>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (sqlServerApplicationStore is not null && relationalStore is not null)
        {
            await sqlServerApplicationStore.EnsureReadyAsync(cancellationToken);
            var relationalStateReady = await sqlServerApplicationStore.IsRelationalStateReadyAsync(cancellationToken);
            var legacyPlaces = relationalStateReady
                ? null
                : await sqlServerApplicationStore.LoadSectionAsync("places", cancellationToken);

            List<VisitorGuide.Infrastructure.Entities.DiaDiemEntity> places;
            if (!string.IsNullOrWhiteSpace(legacyPlaces))
            {
                places = placeFileStore.DeserializeEntities(legacyPlaces);
                await relationalStore.SavePlacesAsync(places, cancellationToken);
            }
            else
            {
                places = await relationalStore.LoadPlacesAsync(cancellationToken);
                if (places.Count == 0 && !relationalStateReady)
                {
                    places = await placeFileStore.LoadEntitiesAsync(cancellationToken);
                    await relationalStore.SavePlacesAsync(places, cancellationToken);
                }
            }

            await dbContext.ReloadFromSqlAsync(places, cancellationToken);
            return;
        }

        if (await dbContext.DiaDiem.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedPlaces = await placeFileStore.LoadEntitiesAsync(cancellationToken);

        dbContext.DiaDiem.AddRange(seedPlaces);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
