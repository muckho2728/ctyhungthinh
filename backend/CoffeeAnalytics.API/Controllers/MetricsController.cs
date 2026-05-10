using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CoffeeAnalytics.API.Controllers;

/// <summary>
/// Basic metrics and observability endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class MetricsController : ControllerBase
{
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static int _requestCount = 0;
    private static readonly object _lock = new();

    /// <summary>Get basic application metrics.</summary>
    [HttpGet]
    public IActionResult GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        
        return Ok(new
        {
            application = new
            {
                name = "CoffeeAnalytics.API",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                uptime = (DateTime.UtcNow - _startTime).ToString(@"dd\.hh\:mm\:ss"),
                startTime = _startTime
            },
            system = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSet = process.WorkingSet64,
                privateMemory = process.PrivateMemorySize64,
                cpuTime = process.TotalProcessorTime
            },
            requests = new
            {
                total = _requestCount,
                timestamp = DateTime.UtcNow
            }
        });
    }

    /// <summary>Increment request counter (called by middleware).</summary>
    public static void IncrementRequestCount()
    {
        lock (_lock)
        {
            _requestCount++;
        }
    }
}
