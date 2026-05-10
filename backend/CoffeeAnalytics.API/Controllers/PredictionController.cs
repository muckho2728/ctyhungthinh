using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Application.Interfaces;

namespace CoffeeAnalytics.API.Controllers;

[ApiController]
[Route("api/prediction")]
public class PredictionController : ControllerBase
{
    private readonly IPredictionService _predictionService;

    public PredictionController(IPredictionService predictionService)
        => _predictionService = predictionService;

    /// <summary>
    /// Get AI price prediction. Premium users get all methods; free users get linear only.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPrediction(
        [FromQuery] string symbol = "KC1",
        [FromQuery] string method = "linear",
        [FromQuery] int horizon = 7,
        CancellationToken ct = default)
    {
        // Future: restrict prophet/lstm to premium users
        var validMethods = new[] { "linear", "prophet", "lstm" };
        if (!validMethods.Contains(method.ToLower()))
            return BadRequest(new { error = $"Invalid method. Use: {string.Join(", ", validMethods)}" });

        horizon = Math.Clamp(horizon, 1, 30);

        var result = await _predictionService.GetPredictionAsync(symbol, method.ToLower(), horizon, ct);
        return Ok(result);
    }
}
