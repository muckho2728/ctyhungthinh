namespace CoffeeAnalytics.Domain.Entities;

/// <summary>
/// Tracks user usage for rate limiting and analytics.
/// </summary>
public class UsageTracking : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public UsageMetricType MetricType { get; set; }
    public int Count { get; set; } = 1;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string? Metadata { get; set; } // JSON
}

public enum UsageMetricType
{
    ApiCall,
    Prediction,
    Alert,
    ChartView,
    DataExport
}

/// <summary>
/// Feature flags for enabling/disabling features per plan.
/// </summary>
public class FeatureFlag : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = false;
    public string? AllowedPlans { get; set; } // JSON array of plan names
    public string? Metadata { get; set; } // JSON
}
