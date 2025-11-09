using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class ApplicationColorRepository(ApplicationDbContext db)
{
    public Task<List<ApplicationBrandColorModel>> GetAllForApplication(ApplicationModel model)
        => GetAllForApplication(model.Id);
    public Task<List<ApplicationBrandColorModel>> GetAllForApplication(Guid appId)
    {
        return db.ApplicationBrandColors.AsNoTracking()
            .Where(e => e.ApplicationId == appId)
            .ToListAsync();
    }

    public Task<ApplicationBrandColorModel?> Get(Guid appId, ApplicationBrandColorType type)
    {
        return db.ApplicationBrandColors.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == appId && e.Type == type);
    }

    public async Task<ApplicationBrandColorModel> InsertOrUpdate(ApplicationBrandColorModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (await ctx.ApplicationBrandColors.AnyAsync(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type))
            {
                await ctx.ApplicationBrandColors.Where(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Value, model.Value)
                    .SetProperty(p => p.UpdatedAt, now));
            }
            else
            {
                model.CreatedAt = now;
                model.UpdatedAt = null;
                await ctx.ApplicationBrandColors.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.ApplicationBrandColors.AsNoTracking()
                .SingleAsync(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
