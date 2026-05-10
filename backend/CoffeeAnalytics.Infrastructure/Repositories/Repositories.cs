using Microsoft.EntityFrameworkCore;
using CoffeeAnalytics.Domain.Entities;
using CoffeeAnalytics.Domain.Interfaces;
using CoffeeAnalytics.Infrastructure.Persistence;

namespace CoffeeAnalytics.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Email == email.ToLower(), ct);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.ToListAsync(ct);

    public async Task AddAsync(User entity, CancellationToken ct = default)
    {
        entity.Email = entity.Email.ToLower();
        await _db.Users.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(User entity, CancellationToken ct = default)
    {
        entity.DeletedAt = DateTime.UtcNow; // soft delete
        await _db.SaveChangesAsync(ct);
    }
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}

public class CommodityPriceRepository : ICommodityPriceRepository
{
    private readonly AppDbContext _db;
    public CommodityPriceRepository(AppDbContext db) => _db = db;

    public async Task<CommodityPrice?> GetLatestAsync(string symbol, CancellationToken ct = default)
        => await _db.CommodityPrices
            .Where(p => p.Symbol == symbol)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<CommodityPrice>> GetHistoryAsync(
        string symbol, string interval, int outputSize, CancellationToken ct = default)
        => await _db.CommodityPrices
            .Where(p => p.Symbol == symbol && p.Interval == interval)
            .OrderByDescending(p => p.Timestamp)
            .Take(outputSize)
            .ToListAsync(ct);

    public async Task AddAsync(CommodityPrice price, CancellationToken ct = default)
    {
        await _db.CommodityPrices.AddAsync(price, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<CommodityPrice> prices, CancellationToken ct = default)
    {
        await _db.CommodityPrices.AddRangeAsync(prices, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public class AlertRepository : IAlertRepository
{
    private readonly AppDbContext _db;
    public AlertRepository(AppDbContext db) => _db = db;

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Alerts.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<Alert>> GetAllAsync(CancellationToken ct = default)
        => await _db.Alerts.ToListAsync(ct);

    public async Task<IEnumerable<Alert>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.Alerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Alert>> GetActiveAlertsForSymbolAsync(string symbol, CancellationToken ct = default)
        => await _db.Alerts
            .Include(a => a.User)
            .Where(a => a.Symbol == symbol && a.Status == AlertStatus.Active)
            .ToListAsync(ct);

    public async Task AddAsync(Alert entity, CancellationToken ct = default)
    {
        await _db.Alerts.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Alert entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _db.Alerts.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Alert entity, CancellationToken ct = default)
    {
        _db.Alerts.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
