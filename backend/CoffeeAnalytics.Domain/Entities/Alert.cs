namespace CoffeeAnalytics.Domain.Entities;

/// <summary>
/// User-defined price alert that triggers a notification.
/// </summary>
public class Alert : BaseEntity
{
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = "KC1";
    public AlertCondition Condition { get; set; }
    public decimal Threshold { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Active;
    public string? Note { get; set; }
    public DateTime? TriggeredAt { get; set; }
    public decimal? TriggeredPrice { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

public enum AlertCondition
{
    Above,
    Below
}

public enum AlertStatus
{
    Active,
    Triggered,
    Disabled
}
