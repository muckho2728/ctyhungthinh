using CoffeeAnalytics.Application.DTOs.Market;

namespace CoffeeAnalytics.Application.Interfaces;

/// <summary>
/// Market data service for real-time and historical coffee price data.
/// </summary>
public interface IMarketService
{
    Task<RealtimePriceDto> GetRealtimePriceAsync(string symbol, CancellationToken ct = default);
    Task<QuoteDto> GetQuoteAsync(string symbol, CancellationToken ct = default);
    Task<ChartDataDto> GetChartDataAsync(string symbol, string interval, int outputSize, CancellationToken ct = default);
    Task<IndicatorsDto> GetIndicatorsAsync(string symbol, string interval, CancellationToken ct = default);
}
