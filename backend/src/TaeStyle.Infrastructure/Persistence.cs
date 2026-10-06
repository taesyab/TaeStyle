using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaeStyle.Domain;
namespace TaeStyle.Infrastructure;

public sealed class Account : IdentityUser<Guid>
{
    public string Currency { get; set; } = "USD";
    public string TimeZone { get; set; } = "America/Guayaquil";
    public int SecurityVersion { get; set; } = 1;
}
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<Account, Guid>(options)
{
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Garment> Garments => Set<Garment>();
    public DbSet<GarmentPhoto> GarmentPhotos => Set<GarmentPhoto>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var photos = builder.Entity<GarmentPhoto>();
        photos.HasKey(x => x.Id);
        photos.HasAlternateKey(x => new { x.Id, x.UserId });
        photos.HasIndex(x => x.ExpiresAt);
        photos.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        var garments = builder.Entity<Garment>();
        garments.HasKey(x => x.Id);
        garments.Property(x => x.Name).HasMaxLength(100);
        garments.Property(x => x.Category).HasMaxLength(30);
        garments.Property(x => x.Color).HasMaxLength(40);
        garments.Property(x => x.Brand).HasMaxLength(100);
        garments.Property(x => x.Condition).HasMaxLength(20);
        garments.Property(x => x.PurchasePrice).HasPrecision(12, 2);
        garments.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id });
        garments.HasIndex(x => x.PhotoId).IsUnique();
        garments.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        garments.HasOne<GarmentPhoto>().WithMany().HasForeignKey(x => new { x.PhotoId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId }).OnDelete(DeleteBehavior.Restrict);
        garments.ToTable(t => t.HasCheckConstraint("CK_Garment_Price", "\"PurchasePrice\" IS NULL OR \"PurchasePrice\" >= 0"));
        builder.Entity<Account>().HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Entity<Account>().Property(x => x.Currency).HasMaxLength(3);
        builder.Entity<Account>().Property(x => x.TimeZone).HasMaxLength(64);
        var sessions = builder.Entity<Session>();
        sessions.HasKey(x => x.Id);
        sessions.Property(x => x.TokenHash).HasMaxLength(64);
        sessions.HasIndex(x => x.TokenHash).IsUnique();
        sessions.HasIndex(x => x.FamilyId);
        sessions.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
