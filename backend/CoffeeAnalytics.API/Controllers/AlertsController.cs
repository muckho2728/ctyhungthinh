using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Application.DTOs.Alerts;
using CoffeeAnalytics.Application.Interfaces;

namespace CoffeeAnalytics.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
        => _alertService = alertService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get all alerts for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        var alerts = await _alertService.GetUserAlertsAsync(CurrentUserId, ct);
        return Ok(alerts);
    }

    /// <summary>Create a new price alert.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertDto), 201)]
    public async Task<IActionResult> CreateAlert(
        [FromBody] CreateAlertRequest request, CancellationToken ct)
    {
        try
        {
            var alert = await _alertService.CreateAlertAsync(CurrentUserId, request, ct);
            return StatusCode(201, alert);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete an alert by ID (only owner can delete).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid id, CancellationToken ct)
    {
        try
        {
            await _alertService.DeleteAlertAsync(id, CurrentUserId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
