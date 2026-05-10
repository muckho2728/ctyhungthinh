using CoffeeAnalytics.Application.DTOs.Alerts;
using CoffeeAnalytics.Application.Interfaces;
using CoffeeAnalytics.Domain.Entities;
using CoffeeAnalytics.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoffeeAnalytics.Infrastructure.Services;

/// <summary>
/// Alert service managing CRUD and real-time threshold checking.
/// Pushes notifications via SignalR when alert triggers.
/// </summary>
public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepo;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IAlertRepository alertRepo, ILogger<AlertService> logger)
    {
        _alertRepo = alertRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<AlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct = default)
    {
        var alerts = await _alertRepo.GetByUserIdAsync(userId, ct);
        return alerts.Select(MapToDto);
    }

    public async Task<AlertDto> CreateAlertAsync(
        Guid userId, CreateAlertRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AlertCondition>(request.Condition, true, out var condition))
            throw new ArgumentException($"Invalid condition: {request.Condition}. Use 'above' or 'below'.");

        var alert = new Alert
        {
            UserId = userId,
            Symbol = request.Symbol.ToUpper(),
            Condition = condition,
            Threshold = request.Threshold,
            Note = request.Note,
            Status = AlertStatus.Active
        };

        await _alertRepo.AddAsync(alert, ct);
        return MapToDto(alert);
    }

    public async Task DeleteAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default)
    {
        var alert = await _alertRepo.GetByIdAsync(alertId, ct)
            ?? throw new KeyNotFoundException("Alert not found.");

        if (alert.UserId != userId)
            throw new UnauthorizedAccessException("Cannot delete another user's alert.");

        await _alertRepo.DeleteAsync(alert, ct);
    }

    public async Task CheckAndTriggerAlertsAsync(
        string symbol, decimal currentPrice, CancellationToken ct = default)
    {
        var activeAlerts = await _alertRepo.GetActiveAlertsForSymbolAsync(symbol, ct);

        foreach (var alert in activeAlerts)
        {
            bool shouldTrigger = alert.Condition switch
            {
                AlertCondition.Above => currentPrice >= alert.Threshold,
                AlertCondition.Below => currentPrice <= alert.Threshold,
                _ => false
            };

            if (shouldTrigger)
            {
                alert.Status = AlertStatus.Triggered;
                alert.TriggeredAt = DateTime.UtcNow;
                alert.TriggeredPrice = currentPrice;

                await _alertRepo.UpdateAsync(alert, ct);

                _logger.LogInformation(
                    "Alert triggered for User {UserId}: {Symbol} {Condition} {Threshold} @ {Price}",
                    alert.UserId, symbol, alert.Condition, alert.Threshold, currentPrice);

                // TODO: Send SignalR notification to user
            }
        }
    }

    private static AlertDto MapToDto(Alert a) => new(
        a.Id, a.Symbol, a.Condition.ToString().ToLower(),
        a.Threshold, a.Status.ToString().ToLower(),
        a.Note, a.TriggeredAt, a.TriggeredPrice, a.CreatedAt
    );
}
