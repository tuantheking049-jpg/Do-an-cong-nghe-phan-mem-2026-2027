using Microsoft.EntityFrameworkCore;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure;

public sealed class VisitorGuideDbContext(DbContextOptions<VisitorGuideDbContext> options) : DbContext(options)
{
    public DbSet<DiaDiemEntity> DiaDiem => Set<DiaDiemEntity>();
    public DbSet<DiaDiemMoTaEntity> MoTaDiaDiem => Set<DiaDiemMoTaEntity>();
    public DbSet<DiaDiemDiemNoiBatEntity> DiemNoiBat => Set<DiaDiemDiemNoiBatEntity>();
    public DbSet<DiaDiemAnhEntity> AnhDiaDiem => Set<DiaDiemAnhEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiaDiemEntity>(entity =>
        {
            entity.ToTable("DiaDiemThamQuan");
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
            entity.ToTable("MoTaDiaDiem");
            entity.HasKey(description => new { description.DiaDiemId, description.LanguageCode });
            entity.Property(description => description.LanguageCode).HasMaxLength(10);
            entity.Property(description => description.Description).IsRequired();
        });

        modelBuilder.Entity<DiaDiemDiemNoiBatEntity>(entity =>
        {
            entity.ToTable("DiemNoiBatDiaDiem");
            entity.HasKey(highlight => highlight.Id);
            entity.HasIndex(highlight => new { highlight.DiaDiemId, highlight.Position }).IsUnique();
            entity.Property(highlight => highlight.Text).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<DiaDiemAnhEntity>(entity =>
        {
            entity.ToTable("AnhBoSungDiaDiem");
            entity.HasKey(photo => photo.Id);
            entity.HasIndex(photo => new { photo.DiaDiemId, photo.Position }).IsUnique();
            entity.Property(photo => photo.Url).HasMaxLength(2048).IsRequired();
            entity.Property(photo => photo.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(photo => photo.Caption).HasMaxLength(300).IsRequired();
        });
    }
}
