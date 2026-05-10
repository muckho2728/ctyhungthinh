using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Infrastructure.Services;
using System;
using System.Threading;

namespace CoffeeAnalytics.API.Controllers;

[ApiController]
[Route("api/pepper")]
public class PepperController : ControllerBase
{
    private readonly PepperService _pepperService;
    private readonly ILogger<PepperController> _logger;

    public PepperController(PepperService pepperService, ILogger<PepperController> logger)
    {
        _pepperService = pepperService;
        _logger = logger;
    }

    /// <summary>
    /// Get domestic pepper prices (Vietnam regions) — scraped live from giatieu.com.
    /// </summary>
    [HttpGet("prices/domestic")]
    public async Task<ActionResult> GetDomesticPrices(
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        try
        {
            var prices = await _pepperService.GetDomesticPricesAsync(forceRefresh: refresh, ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domestic pepper prices");
            return StatusCode(500, new { error = "Failed to get domestic pepper prices" });
        }
    }

    /// <summary>
    /// Get today's pepper price summary (average, daily change, per-region table).
    /// Data source: giatieu.com/gia-tieu-hom-nay.
    /// </summary>
    [HttpGet("prices/summary")]
    public async Task<ActionResult> GetDomesticSummary(
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        try
        {
            var summary = await _pepperService.GetDomesticSummaryAsync(forceRefresh: refresh, ct);
            return Ok(new { data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pepper price summary");
            return StatusCode(500, new { error = "Failed to get pepper price summary" });
        }
    }

    /// <summary>
    /// Get international pepper prices (global markets).
    /// </summary>
    [HttpGet("prices/international")]
    public async Task<ActionResult> GetInternationalPrices(
        [FromQuery] string symbol = "VPA",
        [FromQuery] int outputSize = 30,
        CancellationToken ct = default)
    {
        try
        {
            var prices = await _pepperService.GetInternationalPricesAsync(symbol, outputSize, ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting international pepper prices");
            return StatusCode(500, new { error = "Failed to get international pepper prices" });
        }
    }

    /// <summary>
    /// Get pepper price history for ML prediction.
    /// </summary>
    [HttpGet("prices/history")]
    public async Task<ActionResult> GetPriceHistory(
        [FromQuery] string symbol = "VPA",
        [FromQuery] int days = 90,
        CancellationToken ct = default)
    {
        try
        {
            var prices = await _pepperService.GetPriceHistoryForPredictionAsync(symbol, days, ct);
            return Ok(new { data = prices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pepper price history");
            return StatusCode(500, new { error = "Failed to get price history" });
        }
    }
}
