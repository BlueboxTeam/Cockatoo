using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class StorageFileRepository(ApplicationDbContext db)
{
    public Task<StorageFileModel?> GetById(
        Guid id,
        StorageFileModelInclude include = StorageFileModelInclude.None)
    {
        return GetQueryable(db, include)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    public Task<StorageFileModel?> Get(
        StorageFileModel model,
        StorageFileModelInclude include = StorageFileModelInclude.None)
        => GetById(model.Id, include);

    public Task<bool> Exists(Guid id)
    {
        return db.StorageFiles.AnyAsync(e => e.Id == id);
    }
    public Task<bool> Exists(StorageFileModel model)
        => Exists(model.Id);

    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.StorageFiles
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
    public Task<long> Delete(params IEnumerable<StorageFileModel> files)
        => Delete(files.Select(e => e.Id));

    public async Task<StorageFileModel> InsertOrUpdate(StorageFileModel model,
        StorageFileModelInclude include = StorageFileModelInclude.None)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.StorageFiles.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.StorageFiles.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Sha256Hash, model.Sha256Hash)
                    .SetProperty(p => p.Location, model.Location)
                    .SetProperty(p => p.ContentType, model.ContentType)
                    .SetProperty(p => p.Size, model.Size)
                    .SetProperty(p => p.UpdatedAt, model.UpdatedAt)
                    .SetProperty(p => p.CreatedByUserId, model.CreatedByUserId));
            }
            else
            {
                await ctx.StorageFiles.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await GetQueryable(ctx, include).SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }


    private static IQueryable<StorageFileModel> GetQueryable(ApplicationDbContext db, StorageFileModelInclude include)
    {
        IQueryable<StorageFileModel> q = db.StorageFiles.AsNoTracking();
        if (include.HasFlag(StorageFileModelInclude.CreatedByUser))
        {
            q = q.Include(e => e.CreatedByUser);
        }
        return q;
    }
}

[Flags]
public enum StorageFileModelInclude
{
    None = 0,
    CreatedByUser = 1 << 0,
}