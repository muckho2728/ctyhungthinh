namespace CoffeeAnalytics.Domain.Entities;

using System;

/// <summary>
/// Commodity type for separation of Coffee and Pepper modules
/// </summary>
public enum CommodityType
{
    Coffee = 1,
    Pepper = 2
}

/// <summary>
/// Real-time and historical price snapshot for a commodity symbol.
/// </summary>
public class CommodityPrice
{
    public long Id { get; set; }
    public string Symbol { get; set; } = "KC1";
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
    public decimal? PercentChange { get; set; }
    public DateTime Timestamp { get; set; }
    public string Interval { get; set; } = "1day";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Commodity type to separate Coffee and Pepper data
    /// </summary>
    public CommodityType Type { get; set; } = CommodityType.Coffee;
    
    /// <summary>
    /// Region for domestic prices (e.g., "Đắk Lắk", "Chư Sê")
    /// </summary>
    public string? Region { get; set; }
    
    /// <summary>
    /// Grade/Quality (e.g., "Cà phê nhân loại 1", "Tiêu đen loại 1")
    /// </summary>
    public string? Grade { get; set; }
    
    /// <summary>
    /// Currency unit (e.g., "VND/kg", "USD/ton", "US cents/lb")
    /// </summary>
    public string? Currency { get; set; } = "VND/kg";
}
