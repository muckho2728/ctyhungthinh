namespace CoffeeAnalytics.Domain.Entities;

/// <summary>
/// Subscription plan with pricing and feature limits.
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty; // free, premium, enterprise
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public string? Features { get; set; } // JSON array of features
    public int ApiRateLimitPerMinute { get; set; } = 30;
    public int ApiRateLimitPerDay { get; set; } = 1000;
    public int MaxAlerts { get; set; } = 5;
    public int MaxPredictionsPerDay { get; set; } = 10;
    public int HistoricalDataDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // Navigation
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}

/// <summary>
/// User's active subscription with billing information.
/// </summary>
public class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime CurrentPeriodStart { get; set; } = DateTime.UtcNow;
    public DateTime CurrentPeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(1);
    public bool CancelAtPeriodEnd { get; set; } = false;
    public DateTime? CancelledAt { get; set; }
    public string? PaymentProvider { get; set; } // stripe, paypal
    public string? ProviderSubscriptionId { get; set; }
    public string? Metadata { get; set; } // JSON
}

public enum SubscriptionStatus
{
    Active,
    Cancelled,
    Expired,
    Trialing
}
