using Microsoft.EntityFrameworkCore;
using CoffeeAnalytics.Domain.Entities;
using System.Linq;
using System;

namespace CoffeeAnalytics.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for all application entities.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CommodityPrice> CommodityPrices => Set<CommodityPrice>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use lowercase table names for PostgreSQL
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName().ToLowerInvariant());

            // Use snake_case for column names
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Use snake_case for foreign keys
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            // Use snake_case for indexes
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }

        // ─── User ──────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.ToTable("users");
            e.Property(u => u.Role)
             .HasConversion(
                v => v.ToString().ToLower(),
                v => (UserRole)Enum.Parse(typeof(UserRole), v, true))
             .HasDefaultValue(UserRole.Free);
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(255);
            e.HasQueryFilter(u => u.DeletedAt == null); // global soft-delete filter
        });

        // ─── RefreshToken ──────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.ToTable("refresh_tokens");
            e.Property(rt => rt.Token).HasMaxLength(512).IsRequired();
            e.HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── CommodityPrice ────────────────────────────────
        modelBuilder.Entity<CommodityPrice>(e =>
        {
            e.HasKey(p => p.Id);
            e.ToTable("commodity_prices");
            e.Property(p => p.Id).ValueGeneratedOnAdd();
            e.HasIndex(p => new { p.Symbol, p.Interval, p.Timestamp });
            e.HasIndex(p => p.Type); // Index for filtering by CommodityType
            e.Property(p => p.Symbol).HasMaxLength(20).IsRequired();
            e.Property(p => p.Close).HasPrecision(12, 4).IsRequired();
            e.Property(p => p.Open).HasPrecision(12, 4);
            e.Property(p => p.High).HasPrecision(12, 4);
            e.Property(p => p.Low).HasPrecision(12, 4);
            e.Property(p => p.PercentChange).HasPrecision(8, 4);
            e.Property(p => p.Interval).HasMaxLength(10).HasDefaultValue("1day");
            
            // Enum conversion for CommodityType (Coffee/Pepper)
            e.Property(p => p.Type)
             .HasConversion(
                v => v.ToString().ToLower(),
                v => (CommodityType)Enum.Parse(typeof(CommodityType), v, true))
             .HasDefaultValue(CommodityType.Coffee);
            
            // New fields for domestic prices
            e.Property(p => p.Region).HasMaxLength(100);
            e.Property(p => p.Grade).HasMaxLength(100);
            e.Property(p => p.Currency).HasMaxLength(20).HasDefaultValue("VND/kg");
        });

        // ─── Prediction ────────────────────────────────────
        modelBuilder.Entity<Prediction>(e =>
        {
            e.HasKey(p => p.Id);
            e.ToTable("predictions");
            e.HasIndex(p => new { p.Symbol, p.CreatedAt });
            e.Property(p => p.Symbol).HasMaxLength(20).IsRequired();
            e.Property(p => p.PredictedPrice).HasPrecision(12, 4).IsRequired();
            e.Property(p => p.Confidence).HasPrecision(5, 4).IsRequired();
            e.Property(p => p.Method).HasConversion(
                v => v.ToString().ToLower(),
                v => (PredictionMethod)Enum.Parse(typeof(PredictionMethod), v, true));
            e.Property(p => p.Trend).HasConversion(
                v => v.ToString().ToLower(),
                v => (TrendDirection)Enum.Parse(typeof(TrendDirection), v, true));
        });

        // ─── Alert ─────────────────────────────────────────
        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.ToTable("alerts");
            e.HasIndex(a => new { a.Symbol, a.Status });
            e.Property(a => a.Symbol).HasMaxLength(20).IsRequired();
            e.Property(a => a.Threshold).HasPrecision(12, 4).IsRequired();
            e.Property(a => a.Condition).HasConversion(
                v => v.ToString().ToLower(),
                v => (AlertCondition)Enum.Parse(typeof(AlertCondition), v, true));
            e.Property(a => a.Status).HasConversion(
                v => v.ToString().ToLower(),
                v => (AlertStatus)Enum.Parse(typeof(AlertStatus), v, true));
            e.HasOne(a => a.User)
             .WithMany(u => u.Alerts)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                result.Append('_');
            }
            result.Append(char.ToLower(name[i]));
        }
        return result.ToString();
    }
}
