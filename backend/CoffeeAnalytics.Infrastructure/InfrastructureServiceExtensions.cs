using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using CoffeeAnalytics.Application.Interfaces;
using CoffeeAnalytics.Domain.Interfaces;
using CoffeeAnalytics.Infrastructure.ExternalApis;
using CoffeeAnalytics.Infrastructure.Persistence;
using CoffeeAnalytics.Infrastructure.Repositories;
using CoffeeAnalytics.Infrastructure.Services;
using CoffeeAnalytics.Infrastructure.Settings;

namespace CoffeeAnalytics.Infrastructure;

/// <summary>
/// Registers all infrastructure-layer services into the DI container.
/// Called from API Program.cs.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ─── Settings ──────────────────────────────────────────
        services.Configure<TwelveDataSettings>(config.GetSection(TwelveDataSettings.SectionName));
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.Configure<MlServiceSettings>(config.GetSection(MlServiceSettings.SectionName));
        services.Configure<GiacapheSettings>(config.GetSection(GiacapheSettings.SectionName));
        services.Configure<GiatieuSettings>(config.GetSection(GiatieuSettings.SectionName));

        // ─── Database ──────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
        {
            var conn = config.GetConnectionString("DefaultConnection");
            options.UseNpgsql(conn, npg =>
            {
                npg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                npg.CommandTimeout(30);
            });
        });

        // ─── Redis Cache ───────────────────────────────────────
        var redisConn = config["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(redisConn);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetService<ILogger<IConnectionMultiplexer>>();
                    logger?.LogWarning(ex, "Redis connection failed, will use memory cache only");
                    return null!;
                }
            });
        }

        // ─── Memory Cache (fallback) ───────────────────────────
        services.AddMemoryCache();

        // ─── TwelveData HTTP Client + Polly ───────────────────
        var tdBaseUrl = config["TwelveData:BaseUrl"] ?? "https://api.twelvedata.com";
        services.AddHttpClient<TwelveDataClient>(client =>
        {
            client.BaseAddress = new Uri(tdBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "CoffeeAnalyticsPlatform/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // ─── Giacaphe HTTP Client ─────────────────────────────
        var giacapheBaseUrl = config["Giacaphe:BaseUrl"] ?? "https://giacaphe.com";
        services.AddHttpClient<GiacapheScraper>(client =>
        {
            client.BaseAddress = new Uri(giacapheBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "CoffeeAnalyticsPlatform/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy());

        // ─── Giatieu HTTP Client (pepper prices) ──────────────
        var giatieuBaseUrl = config["Giatieu:BaseUrl"] ?? "https://giatieu.com";
        services.AddHttpClient<GiatieuScraper>(client =>
        {
            client.BaseAddress = new Uri(giatieuBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "CoffeeAnalyticsPlatform/1.0");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
            client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9,en;q=0.8");
        })
        .AddPolicyHandler(GetRetryPolicy());

        // ─── ML Service HTTP Client ────────────────────────────
        var mlBaseUrl = config["MlService:BaseUrl"] ?? "http://ml-service:8000";
        services.AddHttpClient("MlService", client =>
        {
            client.BaseAddress = new Uri(mlBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // ─── Repositories ──────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICommodityPriceRepository, CommodityPriceRepository>();
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // ─── Services ──────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IAlertService, AlertService>();
        
        // ─── Module Services (Coffee & Pepper) ─────────────────
        services.AddScoped<Services.CoffeeService>();
        services.AddScoped<Services.PepperService>();

        return services;
    }

    // ─── Polly Policies ───────────────────────────────────────

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
