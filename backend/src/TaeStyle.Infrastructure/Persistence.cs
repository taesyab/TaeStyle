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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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
