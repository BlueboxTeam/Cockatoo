using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;

public class AUDNRevisionRepository(ApplicationDbContext db)
{
    public Task<AutoUpdaterDotNetRevisionModel?> Get(Guid id)
    {
        return db.AutoUpdaterDotNetRevisions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    public Task<AutoUpdaterDotNetRevisionModel?> GetLatestForApp(Guid applicationId, bool includeDisabled = false)
    {
        return db.AutoUpdaterDotNetRevisions.AsNoTracking()
            .Where(e => e.ApplicationId == applicationId && (includeDisabled || e.IsEnabled))
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }
    public Task<List<AutoUpdaterDotNetRevisionModel>> GetAllForApp(Guid applicationId)
    {
        return db.AutoUpdaterDotNetRevisions.AsNoTracking()
            .Where(e => e.ApplicationId == applicationId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
    public Task<List<AutoUpdaterDotNetRevisionModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public Task<List<AutoUpdaterDotNetRevisionModel>> GetAllUsingFile(Guid fileId)
    {
        return db.AutoUpdaterDotNetRevisions.AsNoTracking()
            .Where(e => e.StorageFileId == fileId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
    public Task<List<AutoUpdaterDotNetRevisionModel>> GetAll()
    {
        return db.AutoUpdaterDotNetRevisions.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
    public Task<bool> Exists(Guid id)
    {
        return db.AutoUpdaterDotNetRevisions.AsNoTracking()
            .AnyAsync(e => e.Id == id);
    }

    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.AutoUpdaterDotNetRevisions
                .Where(e => ids.Contains(e.Id))
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

    public async Task<AutoUpdaterDotNetRevisionModel?> InsertOrUpdate(AutoUpdaterDotNetRevisionModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.AutoUpdaterDotNetRevisions.AnyAsync(e => e.Id == model.Id))
            {
                var now = DateTimeOffset.UtcNow;
                await ctx.AutoUpdaterDotNetRevisions.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.ApplicationId, model.ApplicationId)
                    .SetProperty(p => p.Version, model.Version)
                    .SetProperty(p => p.StorageFileId, model.StorageFileId)
                    .SetProperty(p => p.IsEnabled, model.IsEnabled)
                    .SetProperty(p => p.ExecutablePath, model.ExecutablePath)
                    .SetProperty(p => p.ExecutableLaunchArguments, model.ExecutableLaunchArguments)
                    .SetProperty(p => p.ChangelogUrl, model.ChangelogUrl)
                    .SetProperty(p => p.Mandatory, model.Mandatory)
                    .SetProperty(p => p.MandatoryKind, model.MandatoryKind)
                    .SetProperty(p => p.MandatoryMinimumVersion, model.MandatoryMinimumVersion)
                    .SetProperty(p => p.UpdatedAt, now));
            }
            else
            {
                await ctx.AutoUpdaterDotNetRevisions.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.AutoUpdaterDotNetRevisions
                .AsNoTracking().SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}