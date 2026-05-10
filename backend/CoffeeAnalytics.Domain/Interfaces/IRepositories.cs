using CoffeeAnalytics.Domain.Entities;

namespace CoffeeAnalytics.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);
}

public interface ICommodityPriceRepository
{
    Task<CommodityPrice?> GetLatestAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<CommodityPrice>> GetHistoryAsync(string symbol, string interval, int outputSize, CancellationToken ct = default);
    Task AddAsync(CommodityPrice price, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<CommodityPrice> prices, CancellationToken ct = default);
}

public interface IPredictionRepository
{
    Task<Prediction?> GetLatestAsync(string symbol, CancellationToken ct = default);
    Task AddAsync(Prediction prediction, CancellationToken ct = default);
}

public interface IAlertRepository : IRepository<Alert>
{
    Task<IEnumerable<Alert>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Alert>> GetActiveAlertsForSymbolAsync(string symbol, CancellationToken ct = default);
}
