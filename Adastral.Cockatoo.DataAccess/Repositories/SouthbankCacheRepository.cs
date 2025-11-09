using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class SouthbankCacheRepository(ApplicationDbContext db)
{
    public Task<SouthbankCacheModel?> GetLatest()
    {
        return db.SouthbankCache.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<List<SouthbankCacheModel>> GetAll()
    {
        return db.SouthbankCache.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Delete all records from the database where it's not the latest record (by <see cref="SouthbankCacheModel.CreatedAt"/>)
    /// </summary>
    public async Task<int> Clean()
    {
        var latestIds = await db.SouthbankCache.OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Id)
            .Take(1)
            .ToListAsync();
        if (latestIds.Count < 1) return 0;
        var latestId = latestIds[0];

        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.SouthbankCache.Where(e => e.Id != latestId).ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return count;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<SouthbankCacheModel> InsertOrUpdate(SouthbankCacheModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.SouthbankCache
                .AnyAsync(e => e.Id == model.Id))
            {
                await ctx.SouthbankCache.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.V1, model.V1)
                    .SetProperty(p => p.V2, model.V2)
                    .SetProperty(p => p.V3, model.V3));
            }
            else
            {
                await ctx.SouthbankCache.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.SouthbankCache.AsNoTracking()
                .SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
