using CoffeeAnalytics.Application.DTOs.Prediction;

namespace CoffeeAnalytics.Application.Interfaces;

/// <summary>
/// ML prediction service that communicates with Python FastAPI ML microservice.
/// </summary>
public interface IPredictionService
{
    Task<PredictionDto> GetPredictionAsync(string symbol, string method = "linear", int horizonDays = 7, CancellationToken ct = default);
}
