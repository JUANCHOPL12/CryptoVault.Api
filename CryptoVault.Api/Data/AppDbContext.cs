using CryptoVault.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoVault.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<VaultItem> VaultItems { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Garantizar Email Único
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Relación User -> VaultItems (1 a N)
        modelBuilder.Entity<VaultItem>()
            .HasOne(v => v.User)
            .WithMany(u => u.VaultItems)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación User -> AuditLogs (1 a N)
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}