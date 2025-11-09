using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class ApplicationImageRepository(ApplicationDbContext db)
{
    public Task<List<ApplicationBrandAssetModel>> GetAllForApplication(Guid applicationId)
    {
        return db.ApplicationBrandAssets.AsNoTracking()
            .Where(e => e.ApplicationId == applicationId)
            .ToListAsync();
    }
    public Task<List<ApplicationBrandAssetModel>> GetAllForApplication(ApplicationModel app)
        => GetAllForApplication(app.Id);

    public Task<ApplicationBrandAssetModel?> GetForApplication(Guid applicationId, ApplicationBrandAssetType type)
    {
        return db.ApplicationBrandAssets.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == applicationId && e.Type == type);
    }

    public Task<List<ApplicationBrandAssetModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public Task<List<ApplicationBrandAssetModel>> GetAllUsingFile(Guid storageFileId)
    {
        return db.ApplicationBrandAssets.AsNoTracking()
            .Where(e => e.StorageFileId == storageFileId)
            .ToListAsync();
    }

    public Task<List<ApplicationBrandAssetModel>> GetAll()
    {
        return db.ApplicationBrandAssets.AsNoTracking().ToListAsync();
    }

    public async Task<ApplicationBrandAssetModel> InsertOrUpdate(ApplicationBrandAssetModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (await ctx.ApplicationBrandAssets.AnyAsync(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type))
            {
                await ctx.ApplicationBrandAssets.Where(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.IsManagedFile, model.IsManagedFile)
                    .SetProperty(p => p.Sha256Hash, model.Sha256Hash)
                    .SetProperty(p => p.Url, model.Url)
                    .SetProperty(p => p.StorageFileId, model.StorageFileId)
                    .SetProperty(p => p.UpdatedAt, now));
            }
            else
            {
                model.CreatedAt = now;
                await ctx.ApplicationBrandAssets.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.ApplicationBrandAssets.AsNoTracking()
                .SingleAsync(e => e.ApplicationId == model.ApplicationId && e.Type == model.Type);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
