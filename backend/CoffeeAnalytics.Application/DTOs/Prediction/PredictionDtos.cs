namespace CoffeeAnalytics.Application.DTOs.Prediction;

// ─── Prediction Response ──────────────────────────────────

public record ForecastPointDto(string Date, decimal Price);

public record PredictionDto(
    string Symbol,
    string Method,
    decimal PredictedPrice,
    double Confidence,
    string Trend,     // "bullish" | "bearish" | "neutral"
    int HorizonDays,
    IEnumerable<ForecastPointDto> Forecast,
    DateTime CreatedAt
);

// ─── Request to ML Service ───────────────────────────────

public record MlPredictRequest(
    IEnumerable<decimal> Prices,
    IEnumerable<string> Dates,
    string Method,    // "linear" | "prophet" | "lstm"
    int Horizon
);
