using CoffeeAnalytics.Application.DTOs.Alerts;

namespace CoffeeAnalytics.Application.Interfaces;

/// <summary>
/// Alert management service for creating and checking price alerts.
/// </summary>
public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct = default);
    Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertRequest request, CancellationToken ct = default);
    Task DeleteAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
    Task CheckAndTriggerAlertsAsync(string symbol, decimal currentPrice, CancellationToken ct = default);
}
