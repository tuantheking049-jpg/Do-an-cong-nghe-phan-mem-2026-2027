using Microsoft.EntityFrameworkCore;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure;

public sealed class VisitorGuideDbContext(DbContextOptions<VisitorGuideDbContext> options) : DbContext(options)
{
    public DbSet<AdminAccountEntity> AdminAccounts => Set<AdminAccountEntity>();
    public DbSet<LanguageEntity> Languages => Set<LanguageEntity>();
    public DbSet<PoiEntity> Pois => Set<PoiEntity>();
    public DbSet<NarrationEntity> Narrations => Set<NarrationEntity>();
    public DbSet<TranslationEntity> Translations => Set<TranslationEntity>();
    public DbSet<AudioFileEntity> AudioFiles => Set<AudioFileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAccountEntity>(entity =>
        {
            entity.ToTable("Admin_Accounts");
            entity.HasKey(item => item.AdminID);
            entity.Property(item => item.AdminID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LanguageEntity>(entity =>
        {
            entity.ToTable("Languages");
            entity.HasKey(item => item.LangCode);
        });

        modelBuilder.Entity<PoiEntity>(entity =>
        {
            entity.ToTable("POIs");
            entity.HasKey(item => item.PoiID);
            entity.Property(item => item.PoiID).ValueGeneratedOnAdd();
            entity.Property(item => item.Slug).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(255).IsRequired();
            entity.HasOne(item => item.Narration)
                .WithOne(narration => narration.Poi)
                .HasForeignKey<NarrationEntity>(narration => narration.PoiID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NarrationEntity>(entity =>
        {
            entity.ToTable("Narrations");
            entity.HasKey(item => item.NarrationID);
            entity.Property(item => item.NarrationID).ValueGeneratedOnAdd();
            entity.HasMany(item => item.Translations)
                .WithOne(translation => translation.Narration)
                .HasForeignKey(translation => translation.NarrationID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TranslationEntity>(entity =>
        {
            entity.ToTable("Translations");
            entity.HasKey(item => item.TranslationID);
            entity.Property(item => item.TranslationID).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AudioFileEntity>(entity =>
        {
            entity.ToTable("Audio_Files");
            entity.HasKey(item => item.AudioID);
            entity.Property(item => item.AudioID).ValueGeneratedOnAdd();
        });
    }
}
