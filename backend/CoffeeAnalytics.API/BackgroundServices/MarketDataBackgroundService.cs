using Microsoft.AspNetCore.SignalR;
using CoffeeAnalytics.API.Hubs;
using CoffeeAnalytics.Application.Interfaces;

namespace CoffeeAnalytics.API.BackgroundServices;

/// <summary>
/// Polls TwelveData API every 15 seconds and broadcasts
/// live price updates to connected SignalR clients.
/// </summary>
public class MarketDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<MarketDataBackgroundService> _logger;

    private const string DefaultSymbol = "AAPL";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    public MarketDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<MarketHub> hubContext,
        ILogger<MarketDataBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketData background service started, polling every {Interval}s", PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndBroadcastAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in market data polling");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollAndBroadcastAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var marketService = scope.ServiceProvider.GetRequiredService<IMarketService>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        // Fetch latest price
        var price = await marketService.GetRealtimePriceAsync(DefaultSymbol, ct);
        var quote = await marketService.GetQuoteAsync(DefaultSymbol, ct);

        // Broadcast to all clients subscribed to this symbol
        await _hubContext.Clients
            .Group($"symbol-{DefaultSymbol}")
            .SendAsync("PriceUpdate", new
            {
                symbol = DefaultSymbol,
                price = price.Price,
                change = quote.Change,
                percentChange = quote.PercentChange,
                high = quote.High,
                low = quote.Low,
                volume = quote.Volume,
                timestamp = DateTime.UtcNow
            }, ct);

        // Check and trigger alerts
        if (price.Price > 0)
            await alertService.CheckAndTriggerAlertsAsync(DefaultSymbol, price.Price, ct);

        _logger.LogDebug("Broadcast {Symbol} price: {Price}", DefaultSymbol, price.Price);
    }
}
