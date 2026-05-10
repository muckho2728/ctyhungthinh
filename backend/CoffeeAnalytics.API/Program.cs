using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using CoffeeAnalytics.Infrastructure;
using CoffeeAnalytics.Infrastructure.Settings;
using CoffeeAnalytics.Infrastructure.Persistence;
using CoffeeAnalytics.API.Middleware;
using CoffeeAnalytics.API.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CoffeeAnalytics")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/coffee-analytics-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 100 * 1024 * 1024 // 100MB
    )
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ─────────────────────────────────────────────
var services = builder.Services;
var config = builder.Configuration;

// Controllers
services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ─── API Versioning ───────────────────────────────────────────
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version"),
        new QueryStringApiVersionReader("api-version"));
});

services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Coffee Analytics API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Infrastructure (DB, Redis, TwelveData, Services)
services.AddInfrastructure(config);

// ─── JWT Authentication ────────────────────────────────────
var jwtSettings = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        // Allow JWT via SignalR query string
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────
services.AddCors(opt => opt.AddPolicy("FrontendPolicy", policy =>
{
    policy.WithOrigins(
            config["AllowedOrigins"] ?? "http://localhost:3000",
            "http://localhost",
            "http://localhost:80")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // required for SignalR
}));

// ─── SignalR ───────────────────────────────────────────────
services.AddSignalR();

// ─── Rate Limiting ─────────────────────────────────────────
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));
services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
services.AddInMemoryRateLimiting();

// ─── Global Exception Handler ──────────────────────────────
services.AddTransient<GlobalExceptionHandler>();

// ─── Startup Validation ─────────────────────────────────────
services.AddStartupValidation(config);

// ─── Health Checks ──────────────────────────────────────────
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "liveness" });

// ─── Background Services ───────────────────────────────────
services.AddHostedService<CoffeeAnalytics.API.BackgroundServices.MarketDataBackgroundService>();

// ─── Build App ────────────────────────────────────────────
var app = builder.Build();

// ─── Auto Create DB ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
        Log.Information("Database created successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database creation failed");
    }
}

// ─── Middleware Pipeline ───────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();
app.UseIpRateLimiting();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coffee Analytics API v1"));
}

app.UseHsts();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ─── Health Check Endpoints ─────────────────────────────────
app.MapHealthChecks("/health");

app.MapControllers();
app.MapHub<CoffeeAnalytics.API.Hubs.MarketHub>("/hub/market");

// ─── Graceful Shutdown ──────────────────────────────────────
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is stopping...");
    // Cleanup resources here
});

lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Application has stopped.");
    Log.CloseAndFlush();
});

Log.Information("Coffee Analytics API starting on {Env}", app.Environment.EnvironmentName);
await app.RunAsync();
