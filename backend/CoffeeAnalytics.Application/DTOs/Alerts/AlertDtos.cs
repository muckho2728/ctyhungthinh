namespace CoffeeAnalytics.Application.DTOs.Alerts;

// ─── Request DTOs ─────────────────────────────────────────

public record CreateAlertRequest(
    string Symbol,
    string Condition,  // "above" | "below"
    decimal Threshold,
    string? Note
);

// ─── Response DTOs ────────────────────────────────────────

public record AlertDto(
    Guid Id,
    string Symbol,
    string Condition,
    decimal Threshold,
    string Status,
    string? Note,
    DateTime? TriggeredAt,
    decimal? TriggeredPrice,
    DateTime CreatedAt
);
