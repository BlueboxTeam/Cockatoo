using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

public class GroupRepository(ApplicationDbContext db)
{
    public Task<GroupModel?> GetById(Guid id)
    {
        return db.Groups.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<List<GroupModel>> GetAll()
    {
        return db.Groups.AsNoTracking()
            .OrderByDescending(e => e.Priority)
            .ToListAsync();
    }

    public Task<long> GetAllCount()
    {
        return db.Groups.AsNoTracking().LongCountAsync();
    }

    public Task<List<GroupModel>> GetManyById(params IEnumerable<Guid> ids)
    {
        return db.Groups.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .OrderByDescending(e => e.Priority)
            .ToListAsync();
    }

    public Task<List<GroupModel>> GetMayByName(params string[] names)
    {
        return db.Groups.AsNoTracking()
            .Where(e => names.Contains(e.Name))
            .OrderByDescending(e => e.Priority)
            .ToListAsync();
    }

    public async Task<int> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.Groups
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

    public Task<bool> Exists(Guid id)
    {
        return db.Groups.AnyAsync(e => e.Id == id);
    }

    public async Task<GroupModel> InsertOrUpdate(GroupModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.Groups.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.Groups.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Name, model.Name)
                    .SetProperty(p => p.Priority, model.Priority));
            }
            else
            {
                await ctx.Groups.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return await ctx.Groups.AsNoTracking().SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
