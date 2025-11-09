using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BullseyeAppRepository(ApplicationDbContext db)
{
    public Task<ApplicationBullseyeModel?> GetById(Guid appId)
    {
        return db.ApplicationBullseye
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == appId);
    }

    public Task<bool> Exists(Guid appId)
    {
        return db.ApplicationBullseye.AnyAsync(e => e.ApplicationId == appId);
    }
    public Task<bool> Exists(ApplicationBullseyeModel model)
        => Exists(model.ApplicationId);
    public Task<bool> Exists(ApplicationModel model)
        => Exists(model.Id);

    public Task<int> Delete(params IEnumerable<ApplicationBullseyeModel> applications)
        => Delete(applications.Select(e => e.ApplicationId).Distinct());
    public async Task<int> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.ApplicationBullseye
                .Where(e => ids.Contains(e.ApplicationId))
                .ExecuteDeleteAsync();
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

    public async Task<ApplicationBullseyeModel> InsertOrUpdate(ApplicationBullseyeModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.ApplicationBullseye.AnyAsync(e => e.ApplicationId == model.ApplicationId))
            {
                await ctx.ApplicationBullseye.Where(e => e.ApplicationId == model.ApplicationId)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.LatestRevisionId, model.LatestRevisionId)
                    .SetProperty(p => p.CreatedAt, model.CreatedAt)
                    .SetProperty(p => p.CacheV1, model.CacheV1)
                    .SetProperty(p => p.CacheV2, model.CacheV2));
            }
            else
            {
                await ctx.ApplicationBullseye.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.ApplicationBullseye
                .AsNoTracking()
                .SingleAsync(e => e.ApplicationId == model.ApplicationId);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
