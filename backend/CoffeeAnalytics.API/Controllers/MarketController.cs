using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Application.Interfaces;

namespace CoffeeAnalytics.API.Controllers;

/// <summary>
/// Market data endpoints for realtime price, chart OHLCV, and technical indicators.
/// All data is sourced from TwelveData API via backend (never direct from frontend).
/// </summary>
[ApiController]
[Route("api/market")]
public class MarketController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketController(IMarketService marketService)
        => _marketService = marketService;

    /// <summary>Get current realtime price for a symbol (default: KC1 Coffee).</summary>
    [HttpGet("realtime")]
    public async Task<IActionResult> GetRealtimePrice(
        [FromQuery] string symbol = "KC1",
        CancellationToken ct = default)
    {
        var result = await _marketService.GetRealtimePriceAsync(symbol, ct);
        return Ok(result);
    }

    /// <summary>Get full quote: OHLC, volume, change%, market open status.</summary>
    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote(
        [FromQuery] string symbol = "KC1",
        CancellationToken ct = default)
    {
        var result = await _marketService.GetQuoteAsync(symbol, ct);
        return Ok(result);
    }

    /// <summary>Get OHLCV candlestick data for charting.</summary>
    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] string symbol = "KC1",
        [FromQuery] string interval = "1day",
        [FromQuery] int outputSize = 100,
        CancellationToken ct = default)
    {
        // Validate interval
        var validIntervals = new[] { "1min", "5min", "15min", "30min", "1h", "4h", "1day", "1week" };
        if (!validIntervals.Contains(interval))
            return BadRequest(new { error = $"Invalid interval. Must be one of: {string.Join(", ", validIntervals)}" });

        outputSize = Math.Clamp(outputSize, 1, 5000);

        var result = await _marketService.GetChartDataAsync(symbol, interval, outputSize, ct);
        return Ok(result);
    }

    /// <summary>Get technical indicators: RSI, MACD, SMA, EMA, Bollinger Bands.</summary>
    [HttpGet("indicators")]
    public async Task<IActionResult> GetIndicators(
        [FromQuery] string symbol = "KC1",
        [FromQuery] string interval = "1day",
        CancellationToken ct = default)
    {
        var result = await _marketService.GetIndicatorsAsync(symbol, interval, ct);
        return Ok(result);
    }

    /// <summary>Get historical price data for ML training and analytics.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string symbol = "KC1",
        [FromQuery] int days = 365,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 5000);
        var result = await _marketService.GetChartDataAsync(symbol, "1day", days, ct);
        return Ok(result);
    }
}
