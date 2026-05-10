using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoffeeAnalytics.API.Validation;

/// <summary>
/// Validates required environment variables on application startup.
/// </summary>
public class StartupValidator
{
    public static void Validate(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Required settings
        var requiredSettings = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "Jwt:Secret",
            "TwelveData:ApiKey"
        };

        foreach (var setting in requiredSettings)
        {
            var value = configuration[setting];
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Required configuration '{setting}' is missing or empty.");
            }
        }

        // JWT secret validation
        var jwtSecret = configuration["Jwt:Secret"];
        if (!string.IsNullOrWhiteSpace(jwtSecret) && jwtSecret.Length < 32)
        {
            errors.Add("JWT secret must be at least 32 characters long for security.");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Application startup failed due to configuration errors:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }
    }
}

/// <summary>
/// Extension method to register startup validation.
/// </summary>
public static class StartupValidatorExtensions
{
    public static IServiceCollection AddStartupValidation(this IServiceCollection services, IConfiguration configuration)
    {
        StartupValidator.Validate(configuration);
        return services;
    }
}
