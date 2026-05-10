using Microsoft.AspNetCore.SignalR;

namespace CoffeeAnalytics.API.Hubs;

/// <summary>
/// SignalR hub for real-time market price broadcasting.
/// Clients connect and receive live price updates every ~15s.
/// </summary>
public class MarketHub : Hub
{
    private readonly ILogger<MarketHub> _logger;

    public MarketHub(ILogger<MarketHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to MarketHub: {ConnectionId}", Context.ConnectionId);
        // Add user to their user-specific group for targeted alerts
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from MarketHub: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client calls this to subscribe to a symbol group.</summary>
    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"symbol-{symbol.ToUpper()}");
        _logger.LogDebug("Connection {Id} subscribed to {Symbol}", Context.ConnectionId, symbol);
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"symbol-{symbol.ToUpper()}");
    }
}
