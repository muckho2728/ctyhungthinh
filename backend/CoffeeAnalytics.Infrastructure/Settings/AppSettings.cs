namespace CoffeeAnalytics.Infrastructure.Settings;

public class TwelveDataSettings
{
    public const string SectionName = "TwelveData";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.twelvedata.com";
}

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CoffeeAnalyticsPlatform";
    public string Audience { get; set; } = "CoffeeAnalyticsUsers";
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshExpiryDays { get; set; } = 7;
}

public class MlServiceSettings
{
    public const string SectionName = "MlService";
    public string BaseUrl { get; set; } = "http://ml-service:8000";
}
