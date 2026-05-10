using Microsoft.EntityFrameworkCore;
using CoffeeAnalytics.Domain.Entities;
using CoffeeAnalytics.Domain.Interfaces;
using CoffeeAnalytics.Infrastructure.Persistence;

namespace CoffeeAnalytics.Infrastructure.Repositories;

public class PredictionRepository : IPredictionRepository
{
    private readonly AppDbContext _db;
    public PredictionRepository(AppDbContext db) => _db = db;

    public async Task<Prediction?> GetLatestAsync(string symbol, CancellationToken ct = default)
        => await _db.Predictions
            .Where(p => p.Symbol == symbol)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Prediction prediction, CancellationToken ct = default)
    {
        await _db.Predictions.AddAsync(prediction, ct);
        await _db.SaveChangesAsync(ct);
    }
}
