using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CoffeeAnalytics.Application.DTOs.Market;
using CoffeeAnalytics.Application.Interfaces;
using CoffeeAnalytics.Infrastructure.ExternalApis;
using CoffeeAnalytics.Infrastructure.Settings;
using StackExchange.Redis;
using System.Text.Json;

namespace CoffeeAnalytics.Infrastructure.Services;

/// <summary>
/// Market service with two-tier caching: Redis (primary) → Memory (fallback).
/// </summary>
public class MarketService : IMarketService
{
    private readonly TwelveDataClient _tdClient;
    private readonly IDatabase? _redis;
    private readonly IMemoryCache _memCache;
    private readonly ILogger<MarketService> _logger;

    // Cache TTLs
    private static readonly TimeSpan RealtimeTtl = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan QuoteTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ChartTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan IndicatorTtl = TimeSpan.FromMinutes(5);

    public MarketService(
        TwelveDataClient tdClient,
        IConnectionMultiplexer? redis,
        IMemoryCache memCache,
        ILogger<MarketService> logger)
    {
        _tdClient = tdClient;
        _redis = redis?.GetDatabase();
        _memCache = memCache;
        _logger = logger;
    }

    public async Task<RealtimePriceDto> GetRealtimePriceAsync(string symbol, CancellationToken ct = default)
    {
        var key = $"price:{symbol}";
        return await GetCachedAsync(key, RealtimeTtl,
            () => _tdClient.GetPriceAsync(symbol, ct),
            ct) ?? new RealtimePriceDto(symbol, 0, null, null, null);
    }

    public async Task<QuoteDto> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var key = $"quote:{symbol}";
        return await GetCachedAsync(key, QuoteTtl,
            () => _tdClient.GetQuoteAsync(symbol, ct),
            ct) ?? new QuoteDto(symbol, "Coffee", 0, 0, 0, 0, null, null, null, null, null, false);
    }

    public async Task<ChartDataDto> GetChartDataAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
    {
        var key = $"chart:{symbol}:{interval}:{outputSize}";
        return await GetCachedAsync(key, ChartTtl,
            () => _tdClient.GetTimeSeriesAsync(symbol, interval, outputSize, ct),
            ct) ?? new ChartDataDto(symbol, interval, Array.Empty<CandleDto>());
    }

    public async Task<IndicatorsDto> GetIndicatorsAsync(string symbol, string interval, CancellationToken ct = default)
    {
        var key = $"indicators:{symbol}:{interval}";
        return await GetCachedAsync(key, IndicatorTtl,
            async () =>
            {
                var rsiTask   = _tdClient.GetRsiAsync(symbol, interval, ct: ct);
                var smaTask   = _tdClient.GetSmaAsync(symbol, interval, ct: ct);
                var emaTask   = _tdClient.GetEmaAsync(symbol, interval, ct: ct);
                var macdTask  = _tdClient.GetMacdAsync(symbol, interval, ct);
                var bbandsTask = _tdClient.GetBollingerBandsAsync(symbol, interval, ct);
                await Task.WhenAll(rsiTask, smaTask, emaTask, macdTask, bbandsTask);
                return new IndicatorsDto(
                    rsiTask.Result, macdTask.Result,
                    smaTask.Result, emaTask.Result, bbandsTask.Result);
            },
            ct) ?? new IndicatorsDto(null, null, null, null, null);
    }

    // ─── Two-Tier Cache Helper ─────────────────────────────────

    private async Task<T?> GetCachedAsync<T>(
        string key, TimeSpan ttl, Func<Task<T?>> factory, CancellationToken ct) where T : class
    {
        // 1. Try Redis first
        if (_redis != null)
        {
            try
            {
                var cached = await _redis.StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<T>(cached!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis read failed for key {Key}, falling back to memory cache", key);
            }
        }

        // 2. Try memory cache
        if (_memCache.TryGetValue(key, out T? memCached))
            return memCached;

        // 3. Fetch from source
        var data = await factory();
        if (data == null) return null;

        // 4. Write to both caches
        _memCache.Set(key, data, ttl);

        if (_redis != null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(data);
                await _redis.StringSetAsync(key, serialized, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis write failed for key {Key}", key);
            }
        }

        return data;
    }
}
