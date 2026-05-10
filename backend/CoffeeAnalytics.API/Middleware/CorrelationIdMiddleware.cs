using System.Diagnostics;

namespace CoffeeAnalytics.API.Middleware;

/// <summary>
/// Adds correlation ID to each request for distributed tracing.
/// Generates new ID if not present in request headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Add to response header for client-side tracing
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Add to Activity for OpenTelemetry integration
        Activity.Current?.AddTag("correlation.id", correlationId);

        await _next(context);
    }
}
