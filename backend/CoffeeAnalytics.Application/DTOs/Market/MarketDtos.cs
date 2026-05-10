namespace CoffeeAnalytics.Application.DTOs.Market;

// ─── Realtime Quote ───────────────────────────────────────

public record RealtimePriceDto(
    string Symbol,
    decimal Price,
    decimal? Change,
    decimal? PercentChange,
    string? Timestamp
);

public record QuoteDto(
    string Symbol,
    string Name,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? PreviousClose,
    decimal? Change,
    decimal? PercentChange,
    long? Volume,
    string? Timestamp,
    bool IsMarketOpen
);

// ─── Candlestick / OHLC ──────────────────────────────────

public record CandleDto(
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long? Volume
);

public record ChartDataDto(
    string Symbol,
    string Interval,
    IEnumerable<CandleDto> Candles
);

// ─── Indicators ──────────────────────────────────────────

public record IndicatorValueDto(DateTime Timestamp, decimal? Value);

public record MacdDto(
    DateTime Timestamp,
    decimal? Macd,
    decimal? Signal,
    decimal? Histogram
);

public record BollingerBandDto(
    DateTime Timestamp,
    decimal? Upper,
    decimal? Middle,
    decimal? Lower
);

public record IndicatorsDto(
    IEnumerable<IndicatorValueDto>? Rsi,
    IEnumerable<MacdDto>? Macd,
    IEnumerable<IndicatorValueDto>? Sma,
    IEnumerable<IndicatorValueDto>? Ema,
    IEnumerable<BollingerBandDto>? BollingerBands
);
