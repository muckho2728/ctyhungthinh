using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Infrastructure.Services;
using CoffeeAnalytics.Domain.Entities;
using System;
using System.Threading;

namespace CoffeeAnalytics.API.Controllers;

[ApiController]
[Route("api/coffee")]
public class CoffeeController : ControllerBase
{
    private readonly CoffeeService _coffeeService;
    private readonly ILogger<CoffeeController> _logger;

    public CoffeeController(CoffeeService coffeeService, ILogger<CoffeeController> logger)
    {
        _coffeeService = coffeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get international coffee prices (ICE, Liffe)
    /// </summary>
    [HttpGet("prices/international")]
    public async Task<ActionResult> GetInternationalPrices(
        [FromQuery] string symbol = "RC1",
        [FromQuery] string interval = "1day",
        [FromQuery] int outputSize = 30,
        CancellationToken ct = default)
    {
        try
        {
            var prices = await _coffeeService.GetInternationalPricesAsync(symbol, interval, outputSize, ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting international coffee prices");
            return StatusCode(500, new { error = "Failed to get international coffee prices" });
        }
    }

    /// <summary>
    /// Get domestic coffee prices (Vietnam regions)
    /// </summary>
    [HttpGet("prices/domestic")]
    public async Task<ActionResult> GetDomesticPrices(CancellationToken ct = default)
    {
        try
        {
            var prices = await _coffeeService.GetDomesticPricesAsync(ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domestic coffee prices");
            return StatusCode(500, new { error = "Failed to get domestic coffee prices" });
        }
    }

    /// <summary>
    /// Get coffee price history for ML prediction
    /// </summary>
    [HttpGet("prices/history")]
    public async Task<ActionResult> GetPriceHistory(
        [FromQuery] string symbol = "RC1",
        [FromQuery] int days = 90,
        CancellationToken ct = default)
    {
        try
        {
            var prices = await _coffeeService.GetPriceHistoryForPredictionAsync(symbol, days, ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coffee price history");
            return StatusCode(500, new { error = "Failed to get price history" });
        }
    }
}
