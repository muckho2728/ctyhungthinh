using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CoffeeAnalytics.Application.DTOs.Market;
using CoffeeAnalytics.Infrastructure.Settings;

namespace CoffeeAnalytics.Infrastructure.ExternalApis;

/// <summary>
/// HTTP client for TwelveData API.
/// All requests route through backend to protect the API key.
/// Uses Polly retry (configured in DI) and IMemoryCache.
/// </summary>
public class TwelveDataClient
{
    private readonly HttpClient _http;
    private readonly TwelveDataSettings _settings;
    private readonly ILogger<TwelveDataClient> _logger;

    public TwelveDataClient(
        HttpClient http,
        IOptions<TwelveDataSettings> settings,
        ILogger<TwelveDataClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // ─── Real-time Price ──────────────────────────────────────

    public async Task<RealtimePriceDto?> GetPriceAsync(string symbol, CancellationToken ct = default)
    {
        var url = $"/price?symbol={symbol}&apikey={_settings.ApiKey}";
        return await GetAsync<TdPriceResponse, RealtimePriceDto>(
            url,
            r => new RealtimePriceDto(symbol, decimal.Parse(r.Price), null, null, DateTime.UtcNow.ToString("o")),
            ct);
    }

    // ─── Quote (OHLC + Change) ────────────────────────────────

    public async Task<QuoteDto?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var url = $"/quote?symbol={symbol}&apikey={_settings.ApiKey}";
        return await GetAsync<TdQuoteResponse, QuoteDto>(
            url,
            r => new QuoteDto(
                r.Symbol ?? symbol,
                r.Name ?? "Coffee",
                SafeDecimal(r.Open),
                SafeDecimal(r.High),
                SafeDecimal(r.Low),
                SafeDecimal(r.Close),
                SafeDecimalNullable(r.PreviousClose),
                SafeDecimalNullable(r.Change),
                SafeDecimalNullable(r.PercentChange),
                r.Volume.HasValue ? (long)r.Volume : null,
                r.Datetime,
                r.IsMarketOpen ?? false
            ),
            ct);
    }

    // ─── Time Series (OHLCV candlestick) ──────────────────────

    public async Task<ChartDataDto?> GetTimeSeriesAsync(
        string symbol, string interval, int outputSize = 100, CancellationToken ct = default)
    {
        var url = $"/time_series?symbol={symbol}&interval={interval}&outputsize={outputSize}&apikey={_settings.ApiKey}";

        try
        {
            var response = await _http.GetStringAsync(url, ct);
            var root = JsonSerializer.Deserialize<TdTimeSeriesResponse>(response, JsonOpts);

            if (root?.Values == null)
            {
                _logger.LogWarning("TwelveData time_series returned null values for {Symbol} {Interval}", symbol, interval);
                return null;
            }

            var candles = root.Values.Select(v => new CandleDto(
                DateTime.Parse(v.Datetime),
                decimal.Parse(v.Open),
                decimal.Parse(v.High),
                decimal.Parse(v.Low),
                decimal.Parse(v.Close),
                long.TryParse(v.Volume, out var vol) ? vol : null
            )).OrderBy(c => c.Timestamp);

            return new ChartDataDto(symbol, interval, candles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching time series for {Symbol} {Interval}", symbol, interval);
            return null;
        }
    }

    // ─── RSI ──────────────────────────────────────────────────

    public async Task<IEnumerable<IndicatorValueDto>?> GetRsiAsync(
        string symbol, string interval, int timePeriod = 14, CancellationToken ct = default)
    {
        var url = $"/rsi?symbol={symbol}&interval={interval}&time_period={timePeriod}&apikey={_settings.ApiKey}";
        return await GetIndicatorSeriesAsync(url, ct);
    }

    // ─── SMA ──────────────────────────────────────────────────

    public async Task<IEnumerable<IndicatorValueDto>?> GetSmaAsync(
        string symbol, string interval, int timePeriod = 20, CancellationToken ct = default)
    {
        var url = $"/sma?symbol={symbol}&interval={interval}&time_period={timePeriod}&apikey={_settings.ApiKey}";
        return await GetIndicatorSeriesAsync(url, ct);
    }

    // ─── EMA ──────────────────────────────────────────────────

    public async Task<IEnumerable<IndicatorValueDto>?> GetEmaAsync(
        string symbol, string interval, int timePeriod = 20, CancellationToken ct = default)
    {
        var url = $"/ema?symbol={symbol}&interval={interval}&time_period={timePeriod}&apikey={_settings.ApiKey}";
        return await GetIndicatorSeriesAsync(url, ct);
    }

    // ─── MACD ─────────────────────────────────────────────────

    public async Task<IEnumerable<MacdDto>?> GetMacdAsync(
        string symbol, string interval, CancellationToken ct = default)
    {
        var url = $"/macd?symbol={symbol}&interval={interval}&apikey={_settings.ApiKey}";
        try
        {
            var response = await _http.GetStringAsync(url, ct);
            var root = JsonSerializer.Deserialize<TdMacdResponse>(response, JsonOpts);
            return root?.Values?.Select(v => new MacdDto(
                DateTime.Parse(v.Datetime),
                SafeDecimalNullable(v.Macd),
                SafeDecimalNullable(v.MacdSignal),
                SafeDecimalNullable(v.MacdHist)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching MACD for {Symbol}", symbol);
            return null;
        }
    }

    // ─── Bollinger Bands ──────────────────────────────────────

    public async Task<IEnumerable<BollingerBandDto>?> GetBollingerBandsAsync(
        string symbol, string interval, CancellationToken ct = default)
    {
        var url = $"/bbands?symbol={symbol}&interval={interval}&apikey={_settings.ApiKey}";
        try
        {
            var response = await _http.GetStringAsync(url, ct);
            var root = JsonSerializer.Deserialize<TdBbandsResponse>(response, JsonOpts);
            return root?.Values?.Select(v => new BollingerBandDto(
                DateTime.Parse(v.Datetime),
                SafeDecimalNullable(v.UpperBand),
                SafeDecimalNullable(v.MiddleBand),
                SafeDecimalNullable(v.LowerBand)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching BBands for {Symbol}", symbol);
            return null;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────

    private async Task<TDest?> GetAsync<TSource, TDest>(
        string url, Func<TSource, TDest> mapper, CancellationToken ct)
        where TSource : class
    {
        try
        {
            var response = await _http.GetStringAsync(url, ct);
            var data = JsonSerializer.Deserialize<TSource>(response, JsonOpts);
            return data != null ? mapper(data) : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TwelveData API error for url: {Url}", url.Replace(_settings.ApiKey, "***"));
            return default;
        }
    }

    private async Task<IEnumerable<IndicatorValueDto>?> GetIndicatorSeriesAsync(string url, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetStringAsync(url, ct);
            var root = JsonSerializer.Deserialize<TdIndicatorResponse>(response, JsonOpts);
            return root?.Values?.Select(v => new IndicatorValueDto(
                DateTime.Parse(v.Datetime),
                SafeDecimalNullable(v.Value)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching indicator from: {Url}", url.Replace(_settings.ApiKey, "***"));
            return null;
        }
    }

    private static decimal SafeDecimal(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static decimal? SafeDecimalNullable(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}

// ─── TwelveData Response Models ───────────────────────────────────────────────

file record TdPriceResponse([property: JsonPropertyName("price")] string Price);

file record TdQuoteResponse(
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("open")] string? Open,
    [property: JsonPropertyName("high")] string? High,
    [property: JsonPropertyName("low")] string? Low,
    [property: JsonPropertyName("close")] string? Close,
    [property: JsonPropertyName("previous_close")] string? PreviousClose,
    [property: JsonPropertyName("change")] string? Change,
    [property: JsonPropertyName("percent_change")] string? PercentChange,
    [property: JsonPropertyName("volume")] double? Volume,
    [property: JsonPropertyName("datetime")] string? Datetime,
    [property: JsonPropertyName("is_market_open")] bool? IsMarketOpen
);

file record TdTimeSeriesValue(
    [property: JsonPropertyName("datetime")] string Datetime,
    [property: JsonPropertyName("open")] string Open,
    [property: JsonPropertyName("high")] string High,
    [property: JsonPropertyName("low")] string Low,
    [property: JsonPropertyName("close")] string Close,
    [property: JsonPropertyName("volume")] string? Volume
);

file record TdTimeSeriesResponse(
    [property: JsonPropertyName("values")] List<TdTimeSeriesValue>? Values
);

file record TdIndicatorValue(
    [property: JsonPropertyName("datetime")] string Datetime,
    [property: JsonPropertyName("rsi")] string? Value
);

file record TdIndicatorResponse(
    [property: JsonPropertyName("values")] List<TdIndicatorValue>? Values
);

file record TdMacdValue(
    [property: JsonPropertyName("datetime")] string Datetime,
    [property: JsonPropertyName("macd")] string? Macd,
    [property: JsonPropertyName("macd_signal")] string? MacdSignal,
    [property: JsonPropertyName("macd_hist")] string? MacdHist
);

file record TdMacdResponse(
    [property: JsonPropertyName("values")] List<TdMacdValue>? Values
);

file record TdBbandsValue(
    [property: JsonPropertyName("datetime")] string Datetime,
    [property: JsonPropertyName("upper_band")] string? UpperBand,
    [property: JsonPropertyName("middle_band")] string? MiddleBand,
    [property: JsonPropertyName("lower_band")] string? LowerBand
);

file record TdBbandsResponse(
    [property: JsonPropertyName("values")] List<TdBbandsValue>? Values
);
