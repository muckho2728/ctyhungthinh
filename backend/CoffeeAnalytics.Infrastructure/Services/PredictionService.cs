using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CoffeeAnalytics.Application.DTOs.Prediction;
using CoffeeAnalytics.Application.Interfaces;
using CoffeeAnalytics.Domain.Interfaces;
using CoffeeAnalytics.Infrastructure.Settings;

namespace CoffeeAnalytics.Infrastructure.Services;

/// <summary>
/// Calls Python FastAPI ML microservice for price predictions.
/// Falls back to stored predictions if ML service is unavailable.
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly HttpClient _http;
    private readonly IMarketService _marketService;
    private readonly IPredictionRepository _predRepo;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(
        IHttpClientFactory httpFactory,
        IMarketService marketService,
        IPredictionRepository predRepo,
        ILogger<PredictionService> logger)
    {
        _http = httpFactory.CreateClient("MlService");
        _marketService = marketService;
        _predRepo = predRepo;
        _logger = logger;
    }

    public async Task<PredictionDto> GetPredictionAsync(
        string symbol, string method = "linear", int horizonDays = 7, CancellationToken ct = default)
    {
        try
        {
            // Get recent historical data to send to ML service
            var chart = await _marketService.GetChartDataAsync(symbol, "1day", 90, ct);
            var prices = chart.Candles.Select(c => c.Close).ToList();
            var dates = chart.Candles.Select(c => c.Timestamp.ToString("yyyy-MM-dd")).ToList();

            if (prices.Count < 10)
                return GetFallbackPrediction(symbol, prices.LastOrDefault());

            var request = new MlPredictRequest(prices, dates, method, horizonDays);
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync("/predict", content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<MlPredictResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null) return GetFallbackPrediction(symbol, prices.LastOrDefault());

            // Persist prediction to DB
            var prediction = new Domain.Entities.Prediction
            {
                Symbol = symbol,
                Method = method switch
                {
                    "prophet" => Domain.Entities.PredictionMethod.Prophet,
                    "lstm" => Domain.Entities.PredictionMethod.Lstm,
                    _ => Domain.Entities.PredictionMethod.LinearRegression
                },
                PredictedPrice = result.PredictedPrice,
                Confidence = (decimal)result.Confidence,
                Trend = result.Trend.ToLower() switch
                {
                    "bullish" => Domain.Entities.TrendDirection.Bullish,
                    "bearish" => Domain.Entities.TrendDirection.Bearish,
                    _ => Domain.Entities.TrendDirection.Neutral
                },
                ForecastDataJson = JsonSerializer.Serialize(result.Forecast),
                HorizonDays = horizonDays,
                TargetDate = DateTime.UtcNow.AddDays(horizonDays)
            };

            await _predRepo.AddAsync(prediction, ct);

            return new PredictionDto(
                symbol, method, result.PredictedPrice, result.Confidence,
                result.Trend, horizonDays,
                result.Forecast?.Select(f => new ForecastPointDto(f.Date, f.Price)) ?? [],
                DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML prediction failed for {Symbol}, returning last stored prediction", symbol);

            // Fallback: return last stored prediction
            var last = await _predRepo.GetLatestAsync(symbol, ct);
            if (last != null)
            {
                var forecast = string.IsNullOrEmpty(last.ForecastDataJson)
                    ? []
                    : JsonSerializer.Deserialize<List<ForecastPointDto>>(last.ForecastDataJson) ?? [];

                return new PredictionDto(
                    last.Symbol, last.Method.ToString(),
                    last.PredictedPrice, (double)last.Confidence,
                    last.Trend.ToString(), last.HorizonDays,
                    forecast, last.CreatedAt
                );
            }

            return GetFallbackPrediction(symbol, 0);
        }
    }

    private static PredictionDto GetFallbackPrediction(string symbol, decimal lastPrice) =>
        new(symbol, "linear", lastPrice, 0.5, "Neutral", 7, [], DateTime.UtcNow);

    private record MlPredictResponse(
        decimal PredictedPrice,
        double Confidence,
        string Trend,
        List<MlForecastPoint>? Forecast
    );

    private record MlForecastPoint(string Date, decimal Price);
}
