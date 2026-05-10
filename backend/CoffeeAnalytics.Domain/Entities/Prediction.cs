namespace CoffeeAnalytics.Domain.Entities;

/// <summary>
/// AI-generated price prediction result stored for history/trending.
/// </summary>
public class Prediction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = "KC1";
    public PredictionMethod Method { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal Confidence { get; set; } // 0.0 - 1.0
    public TrendDirection Trend { get; set; }
    public string? ForecastDataJson { get; set; } // JSON: [{date, price}]
    public int HorizonDays { get; set; } = 7;
    public DateTime? TargetDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PredictionMethod
{
    LinearRegression,
    Prophet,
    Lstm
}

public enum TrendDirection
{
    Bullish,
    Bearish,
    Neutral
}
