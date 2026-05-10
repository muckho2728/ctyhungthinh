using CoffeeAnalytics.Domain.Entities;
using CoffeeAnalytics.Infrastructure.ExternalApis;
using CoffeeAnalytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoffeeAnalytics.Infrastructure.Services;

/// <summary>
/// Service for Pepper module — handles domestic and international pepper prices.
/// Domestic prices are scraped live from <c>giatieu.com</c> (updated daily).
/// International / historical prices use DB cache + synthetic data for ML training.
/// </summary>
public class PepperService
{
    private readonly AppDbContext _context;
    private readonly GiatieuScraper _giatieuScraper;
    private readonly ILogger<PepperService> _logger;

    public PepperService(
        AppDbContext context,
        GiatieuScraper giatieuScraper,
        ILogger<PepperService> logger)
    {
        _context = context;
        _giatieuScraper = giatieuScraper;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    // Domestic prices — live from giatieu.com
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns today's domestic pepper prices per region.
    /// Scrapes giatieu.com and persists new records; returns cached DB rows
    /// when they are already fresh (same UTC date AND price ≥ 100,000 VNĐ).
    /// Regions: Chư Sê, Đắk Lắk, Đắk Nông, Bình Phước, Bà Rịa – Vũng Tàu, Đồng Nai.
    /// Pass <paramref name="forceRefresh"/> = true to bypass cache and re-scrape.
    /// </summary>
    public async Task<List<CommodityPrice>> GetDomesticPricesAsync(
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        // ── 1. Purge stale/outdated records saved with old synthetic prices (< 100,000 VNĐ) ──
        var staleToday = await _context.CommodityPrices
            .Where(p => p.Type == CommodityType.Pepper
                     && p.Region != null
                     && p.Timestamp.Date == todayUtc
                     && p.Close < 100_000m)
            .ToListAsync(ct);

        if (staleToday.Count > 0)
        {
            _context.CommodityPrices.RemoveRange(staleToday);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Purged {Count} stale domestic pepper records (old synthetic prices)", staleToday.Count);
        }

        // ── 2. Return valid cached rows if already fetched today (unless forceRefresh) ──
        if (!forceRefresh)
        {
            var cached = await _context.CommodityPrices
                .Where(p => p.Type == CommodityType.Pepper
                         && p.Region != null
                         && p.Timestamp.Date == todayUtc
                         && p.Close >= 100_000m)
                .OrderByDescending(p => p.Timestamp)
                .ToListAsync(ct);

            if (cached.Count > 0)
            {
                _logger.LogDebug("Returning {Count} cached domestic pepper prices for {Date}", cached.Count, todayUtc);
                return cached;
            }
        }

        // ── 2. Scrape live data ──
        var scrapeResult = await _giatieuScraper.GetTodayPricesAsync(ct);
        var now = DateTime.UtcNow;
        var prices = new List<CommodityPrice>();

        foreach (var regionPrice in scrapeResult.Prices)
        {
            // Compute % change vs yesterday's closing price for this region
            var previousPrice = await _context.CommodityPrices
                .Where(p => p.Region == regionPrice.Region
                         && p.Type == CommodityType.Pepper
                         && p.Timestamp.Date < todayUtc)
                .OrderByDescending(p => p.Timestamp)
                .Select(p => p.Close)
                .FirstOrDefaultAsync(ct);

            decimal? percentChange = null;
            if (previousPrice > 0)
                percentChange = (regionPrice.Price - previousPrice) / previousPrice * 100m;

            prices.Add(new CommodityPrice
            {
                Symbol = "VN_PEPPER",
                Open = regionPrice.Price - regionPrice.DailyChange,
                High = regionPrice.Price,
                Low = regionPrice.Price - Math.Abs(regionPrice.DailyChange),
                Close = regionPrice.Price,
                Timestamp = now,
                Interval = "1day",
                Type = CommodityType.Pepper,
                Region = regionPrice.Region,
                Grade = regionPrice.Grade,
                Currency = regionPrice.Unit,
                PercentChange = percentChange,
                CreatedAt = now
            });
        }

        if (prices.Count > 0)
        {
            _context.CommodityPrices.AddRange(prices);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Saved {Count} domestic pepper prices from giatieu.com. Average: {Avg:N0} VNĐ/kg",
                prices.Count, scrapeResult.AveragePrice);
        }

        return prices;
    }

    /// <summary>
    /// Returns a summary DTO with today's average pepper price, daily change,
    /// and source metadata — suitable for dashboard cards.
    /// Pass <paramref name="forceRefresh"/> = true to bypass scraper cache.
    /// </summary>
    public async Task<PepperPriceSummary> GetDomesticSummaryAsync(
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var scrapeResult = await _giatieuScraper.GetTodayPricesAsync(ct);

        return new PepperPriceSummary
        {
            Date = scrapeResult.PriceDate,
            AveragePrice = scrapeResult.AveragePrice > 0
                ? scrapeResult.AveragePrice
                : scrapeResult.Prices.Any()
                    ? scrapeResult.Prices.Average(p => p.Price)
                    : 0m,
            DailyChange = scrapeResult.DailyChange,
            PercentChange = scrapeResult.AveragePrice > 0 && scrapeResult.DailyChange != 0
                ? scrapeResult.DailyChange / (scrapeResult.AveragePrice - scrapeResult.DailyChange) * 100m
                : 0m,
            HighestPrice = scrapeResult.Prices.Any() ? scrapeResult.Prices.Max(p => p.Price) : 0m,
            LowestPrice = scrapeResult.Prices.Any() ? scrapeResult.Prices.Min(p => p.Price) : 0m,
            RegionCount = scrapeResult.Prices.Count,
            Source = scrapeResult.Source,
            FetchedAt = scrapeResult.FetchedAt,
            Regions = scrapeResult.Prices.Select(r => new PepperRegionSummary
            {
                Region = r.Region,
                Price = r.Price,
                DailyChange = r.DailyChange,
                Grade = r.Grade,
                Unit = r.Unit
            }).ToList()
        };
    }

    // ──────────────────────────────────────────────────────────────
    // International prices — DB cache + synthetic fallback
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Get international pepper export prices.
    /// </summary>
    public async Task<List<CommodityPrice>> GetInternationalPricesAsync(
        string symbol = "VPA",
        int outputSize = 30,
        CancellationToken ct = default)
    {
        var cachedPrices = await _context.CommodityPrices
            .Where(p => p.Symbol == symbol
                     && p.Type == CommodityType.Pepper
                     && p.Region == null)
            .OrderByDescending(p => p.Timestamp)
            .Take(outputSize)
            .ToListAsync(ct);

        if (cachedPrices.Count >= outputSize)
            return cachedPrices.OrderBy(p => p.Timestamp).ToList();

        // Generate synthetic price series for ML/charting purposes
        var newPrices = new List<CommodityPrice>();
        var now = DateTime.UtcNow;
        var random = new Random();
        var lastPrice = cachedPrices.Any() ? cachedPrices.First().Close : 3_500m;

        for (int i = 0; i < outputSize; i++)
        {
            var timestamp = now.AddDays(-i);
            var change = (decimal)(random.NextDouble() * 100 - 50);
            var price = lastPrice + change;
            var open = price - change / 2;
            var high = Math.Max(Math.Max(open, price), price + Math.Abs(change / 3));
            var low = Math.Min(Math.Min(open, price), price - Math.Abs(change / 3));

            newPrices.Add(new CommodityPrice
            {
                Symbol = symbol,
                Close = price,
                Open = open,
                High = high,
                Low = low,
                Timestamp = timestamp,
                Interval = "1day",
                Type = CommodityType.Pepper,
                Currency = "USD/ton",
                CreatedAt = now
            });

            lastPrice = price;
        }

        if (newPrices.Any())
        {
            _context.CommodityPrices.AddRange(newPrices);
            await _context.SaveChangesAsync(ct);
        }

        return newPrices.OrderBy(p => p.Timestamp).ToList();
    }

    // ──────────────────────────────────────────────────────────────
    // Historical data for ML prediction
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Get price history for ML prediction (up to 3 years of daily data).
    /// Uses domestic reference base price updated from giatieu.com.
    /// </summary>
    public async Task<List<CommodityPrice>> GetPriceHistoryForPredictionAsync(
        string symbol,
        int outputSize = 1095,
        CancellationToken ct = default)
    {
        var cachedData = await _context.CommodityPrices
            .Where(p => p.Symbol == symbol && p.Type == CommodityType.Pepper)
            .OrderByDescending(p => p.Timestamp)
            .Take(outputSize)
            .ToListAsync(ct);

        if (cachedData.Count >= outputSize)
            return cachedData.ToList();

        // Use latest scraped average as base; fallback to 142,800 (09/05/2026 reference)
        var scrapeResult = await _giatieuScraper.GetTodayPricesAsync(ct);
        var basePrice = scrapeResult.AveragePrice > 0 ? scrapeResult.AveragePrice : 142_800m;

        var now = DateTime.UtcNow;
        var historicalPrices = new List<CommodityPrice>();

        for (int i = outputSize - 1; i >= 0; i--)
        {
            var timestamp = now.AddDays(-i);
            var rng = new Random(i);

            var yearFactor = (outputSize - i) / 365.0;
            var trendFactor = 1m + (decimal)(yearFactor * 0.08);           // 8% growth / 3 yr
            var seasonalFactor = 1m + (decimal)(Math.Sin((i % 365) / 365.0 * 2 * Math.PI) * 0.04);
            var randomVariation = (decimal)(rng.NextDouble() * 1_500 - 750);

            var price = basePrice * trendFactor * seasonalFactor + randomVariation;
            var open = price * (1m + (decimal)(rng.NextDouble() * 0.02 - 0.01));
            var high = Math.Max(price * (1m + (decimal)(rng.NextDouble() * 0.03)), Math.Max(open, price));
            var low = Math.Min(price * (1m - (decimal)(rng.NextDouble() * 0.03)), Math.Min(open, price));

            historicalPrices.Add(new CommodityPrice
            {
                Symbol = symbol,
                Close = price,
                Open = open,
                High = high,
                Low = low,
                Volume = rng.Next(1_000, 5_000),
                Timestamp = timestamp,
                Interval = "1day",
                Type = CommodityType.Pepper,
                Grade = "Tiêu đen loại 1",
                Currency = "VND/kg",
                CreatedAt = now
            });
        }

        _context.CommodityPrices.AddRange(historicalPrices);
        await _context.SaveChangesAsync(ct);

        return historicalPrices;
    }
}

// ──────────────────────────────────────────────────────────────────
// Summary DTOs (returned by controller / dashboard endpoints)
// ──────────────────────────────────────────────────────────────────

public class PepperPriceSummary
{
    public DateTime Date { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal DailyChange { get; set; }
    public decimal PercentChange { get; set; }
    public decimal HighestPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public int RegionCount { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; }
    public List<PepperRegionSummary> Regions { get; set; } = new();
}

public class PepperRegionSummary
{
    public string Region { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DailyChange { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
