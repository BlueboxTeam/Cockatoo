using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class ApplicationRepository(ApplicationDbContext db)
{
    public Task<ApplicationModel?> GetById(
        Guid id,
        ApplicationModelInclude include = ApplicationModelInclude.Default)
    {
        return GetQueryable(db, include)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public Task<List<ApplicationModel>> GetManyById(params IEnumerable<Guid> ids)
        => GetManyById(ApplicationModelInclude.Default, ids);
    public Task<List<ApplicationModel>> GetManyById(
        ApplicationModelInclude include,
        params IEnumerable<Guid> ids)
    {
        return GetQueryable(db, include)
            .Where(e => ids.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();
    }

    public Task<bool> ExistsById(Guid id)
    {
        return db.Applications.AnyAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<int> DeleteById(UserModel? deletedByUser, params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.Applications
                .Where(e => ids.Contains(e.Id))
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.DeletedByUserId, deletedByUser?.Id));
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

    public Task<List<ApplicationModel>> GetAll(
        bool includePrivate = true,
        ApplicationModelInclude include = ApplicationModelInclude.Default)
    {
        return GetQueryable(db, include)
            .Where(e => includePrivate || (!e.IsPrivate && !e.IsHidden))
            .ToListAsync();

        throw new NotImplementedException();
    }

    /// <summary>
    /// Insert or Update the <paramref name="model"/> provided.
    /// </summary>
    public async Task<ApplicationModel> InsertOrUpdate(ApplicationModel model,
        ApplicationModelInclude include = ApplicationModelInclude.Default)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.Applications.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.Applications.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.LatestVersion, model.LatestVersion)
                    .SetProperty(p => p.DisplayName, model.DisplayName)
                    .SetProperty(p => p.Type, model.Type)
                    .SetProperty(p => p.IsPrivate, model.IsPrivate)
                    .SetProperty(p => p.IsHidden, model.IsHidden)
                    .SetProperty(p => p.IsManaged, model.IsManaged)
                    .SetProperty(p => p.IsDeleted, model.IsDeleted)
                    .SetProperty(p => p.CreatedAt, model.CreatedAt)
                    .SetProperty(p => p.UpdatedAt, model.UpdatedAt)
                    .SetProperty(p => p.DeletedAt, model.DeletedAt)
                    .SetProperty(p => p.CreatedByUserId, model.CreatedByUserId)
                    .SetProperty(p => p.UpdatedByUserId, model.UpdatedByUserId)
                    .SetProperty(p => p.DeletedByUserId, model.DeletedByUserId));
            }
            else
            {
                await ctx.Applications.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return await GetQueryable(db, include).SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    private static IQueryable<ApplicationModel> GetQueryable(
        ApplicationDbContext db,
        ApplicationModelInclude include = ApplicationModelInclude.Default)
    {
        IQueryable<ApplicationModel> q = db.Applications.AsNoTracking();
        if (include.HasFlag(ApplicationModelInclude.SourceMod))
        {
            q = q.Include(e => e.SourceMod);
        }
        if (include.HasFlag(ApplicationModelInclude.BrandColors))
        {
            q = q.Include(e => e.BrandColors);
        }
        if (include.HasFlag(ApplicationModelInclude.BrandAssets))
        {
            q = q.Include(e => e.BrandAssets);
        }
        return q;
    }
}

[Flags]
public enum ApplicationModelInclude
{
    None = 0,

    SourceMod = 1 << 0,
    BrandColors = 1 << 2,
    BrandAssets = 1 << 3,


    Default = SourceMod
}