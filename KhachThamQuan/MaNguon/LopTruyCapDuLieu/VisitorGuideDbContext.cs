using Microsoft.EntityFrameworkCore;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure;

public sealed class VisitorGuideDbContext(
    DbContextOptions<VisitorGuideDbContext> options,
    JsonPlaceFileStore placeFileStore,
    SqlServerRelationalStore? sqlServerRelationalStore = null) : DbContext(options)
{
    private bool _skipPersistence;

    public DbSet<DiaDiemEntity> DiaDiem => Set<DiaDiemEntity>();
    public DbSet<DiaDiemMoTaEntity> MoTaDiaDiem => Set<DiaDiemMoTaEntity>();
    public DbSet<DiaDiemDiemNoiBatEntity> DiemNoiBat => Set<DiaDiemDiemNoiBatEntity>();
    public DbSet<DiaDiemAnhEntity> AnhDiaDiem => Set<DiaDiemAnhEntity>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveAndPersistAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) =>
        SaveAndPersistAsync(acceptAllChangesOnSuccess, cancellationToken);

    private async Task<int> SaveAndPersistAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken)
    {
        var changes = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (_skipPersistence)
        {
            return changes;
        }

        if (sqlServerRelationalStore is not null)
        {
            var places = await DiaDiem
                .AsNoTracking()
                .Include(place => place.Descriptions)
                .Include(place => place.Highlights)
                .Include(place => place.GalleryPhotos)
                .ToListAsync(cancellationToken);
            await sqlServerRelationalStore.SavePlacesAsync(places, cancellationToken);
        }
        else
        {
            await placeFileStore.SaveAsync(this, cancellationToken);
        }
        return changes;
    }

    public async Task ReloadFromSqlAsync(
        IReadOnlyCollection<DiaDiemEntity> places,
        CancellationToken cancellationToken = default)
    {
        _skipPersistence = true;
        try
        {
            ChangeTracker.Clear();
            await Database.EnsureDeletedAsync(cancellationToken);
            await Database.EnsureCreatedAsync(cancellationToken);
            DiaDiem.AddRange(places);
            await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _skipPersistence = false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiaDiemEntity>(entity =>
        {
            entity.HasKey(place => place.Id);
            entity.Property(place => place.Id).HasMaxLength(100);
            entity.Property(place => place.Name).HasMaxLength(250).IsRequired();
            entity.Property(place => place.Category).HasMaxLength(100).IsRequired();
            entity.Property(place => place.Province).HasMaxLength(150).IsRequired();
            entity.Property(place => place.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(place => place.ImageSourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(place => place.ImagePosition).HasMaxLength(100).IsRequired();
            entity.HasMany(place => place.Descriptions)
                .WithOne(description => description.DiaDiem)
                .HasForeignKey(description => description.DiaDiemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(place => place.Highlights)
                .WithOne(highlight => highlight.DiaDiem)
                .HasForeignKey(highlight => highlight.DiaDiemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(place => place.GalleryPhotos)
                .WithOne(photo => photo.DiaDiem)
                .HasForeignKey(photo => photo.DiaDiemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiaDiemMoTaEntity>(entity =>
        {
            entity.HasKey(description => new { description.DiaDiemId, description.LanguageCode });
            entity.Property(description => description.LanguageCode).HasMaxLength(10);
            entity.Property(description => description.Description).IsRequired();
        });

        modelBuilder.Entity<DiaDiemDiemNoiBatEntity>(entity =>
        {
            entity.HasKey(highlight => highlight.Id);
            entity.HasIndex(highlight => new { highlight.DiaDiemId, highlight.Position }).IsUnique();
            entity.Property(highlight => highlight.Text).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<DiaDiemAnhEntity>(entity =>
        {
            entity.HasKey(photo => photo.Id);
            entity.HasIndex(photo => new { photo.DiaDiemId, photo.Position }).IsUnique();
            entity.Property(photo => photo.Url).HasMaxLength(2048).IsRequired();
            entity.Property(photo => photo.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(photo => photo.Caption).HasMaxLength(300).IsRequired();
        });
    }
}
