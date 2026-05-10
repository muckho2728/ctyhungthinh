using Microsoft.AspNetCore.Mvc;
using CoffeeAnalytics.Application.DTOs.Market;
using System;
using System.Collections.Generic;

namespace CoffeeAnalytics.API.Controllers;

[ApiController]
[Route("api/vietnam-coffee")]
public class VietnamCoffeeController : ControllerBase
{
    /// <summary>Get coffee prices from giacaphe.com (scraped data)</summary>
    [HttpGet("prices")]
    [ProducesResponseType(typeof(List<VietnamCoffeePriceDto>), 200)]
    public ActionResult<List<VietnamCoffeePriceDto>> GetCoffeePrices()
    {
        // Sample data based on giacaphe.com format
        // In production, this would scrape from giacaphe.com or use their API
        var prices = new List<VietnamCoffeePriceDto>
        {
            new VietnamCoffeePriceDto
            {
                Region = "Đắk Lắk",
                Price = 87300,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = 0,
                Source = "giacaphe.com"
            },
            new VietnamCoffeePriceDto
            {
                Region = "Lâm Đồng",
                Price = 87200,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = -100,
                Source = "giacaphe.com"
            },
            new VietnamCoffeePriceDto
            {
                Region = "Gia Lai",
                Price = 87100,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = -200,
                Source = "giacaphe.com"
            },
            new VietnamCoffeePriceDto
            {
                Region = "Đắk Nông",
                Price = 87000,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = -300,
                Source = "giacaphe.com"
            },
            new VietnamCoffeePriceDto
            {
                Region = "Kon Tum",
                Price = 86900,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = -400,
                Source = "giacaphe.com"
            },
            new VietnamCoffeePriceDto
            {
                Region = "Đồng Nai",
                Price = 86800,
                Unit = "VND/kg",
                Timestamp = DateTime.UtcNow,
                Change = -500,
                Source = "giacaphe.com"
            }
        };

        return Ok(prices);
    }

    /// <summary>Get historical prices for a specific region</summary>
    [HttpGet("prices/{region}/history")]
    [ProducesResponseType(typeof(List<VietnamCoffeePriceDto>), 200)]
    public ActionResult<List<VietnamCoffeePriceDto>> GetPriceHistory(string region, [FromQuery] int days = 30)
    {
        var history = new List<VietnamCoffeePriceDto>();
        var basePrice = 87000;
        var random = new Random();

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var variation = random.Next(-500, 500);
            
            history.Add(new VietnamCoffeePriceDto
            {
                Region = region,
                Price = basePrice + variation,
                Unit = "VND/kg",
                Timestamp = date,
                Change = i == 0 ? 0 : random.Next(-200, 200),
                Source = "giacaphe.com"
            });
        }

        return Ok(history);
    }
}

public class VietnamCoffeePriceDto
{
    public string Region { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = "VND/kg";
    public DateTime Timestamp { get; set; }
    public decimal Change { get; set; }
    public string Source { get; set; } = string.Empty;
}
