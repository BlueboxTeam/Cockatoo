using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

public class GroupPermissionGlobalRepository(ApplicationDbContext db)
{
    public Task<GroupPermissionGlobalModel?> GetById(
        Guid id,
        GroupPermissionGlobalInclude include = GroupPermissionGlobalInclude.None)
    {
        return GetQueryable(db, include).FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<List<GroupPermissionGlobalModel>> GetManyBy(
        Guid? id = null,
        Guid? groupId = null,
        PermissionKind[]? kinds = null,
        bool? allow = null,
        GroupPermissionGlobalInclude include = GroupPermissionGlobalInclude.None)
    {
        var query = GetQueryable(db, include);

        if (id.HasValue)
        {
            query = query.Where(e => e.Id == id.Value);
        }
        if (groupId.HasValue)
        {
            query = query.Where(e => e.GroupId == groupId.Value);
        }
        if (kinds != null)
        {
            query = query.Where(e => kinds.Contains(e.Kind));
        }
        if (allow.HasValue)
        {
            query = query.Where(e => e.Allow == allow.Value);
        }
        return query.ToListAsync();
    }

    public Task<List<GroupPermissionGlobalModel>> GetManyById(
        params IEnumerable<Guid> ids)
        => GetManyById(GroupPermissionGlobalInclude.None, ids);
    public Task<List<GroupPermissionGlobalModel>> GetManyById(
        GroupPermissionGlobalInclude include, 
        params IEnumerable<Guid> ids)
    {
        return GetQueryable(db, include)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();
    }

    public Task<List<GroupPermissionGlobalModel>> GetManyByGroup(params IEnumerable<Guid> groupIds)
    {
        return GetManyByGroup(GroupPermissionGlobalInclude.None, groupIds);
    }
    public Task<List<GroupPermissionGlobalModel>> GetManyByGroup(
        GroupPermissionGlobalInclude include,
        params IEnumerable<Guid> groupIds)
    {
        return GetQueryable(db, include)
            .Where(e => groupIds.Contains(e.GroupId))
            .ToListAsync();
    }

    public async Task<int> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.GroupGlobalPermissions
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
        return db.GroupGlobalPermissions.AnyAsync(e => e.Id == id);
    }

    public async Task<GroupPermissionGlobalModel> InsertOrUpdate(
        GroupPermissionGlobalModel model,
        GroupPermissionGlobalInclude include = GroupPermissionGlobalInclude.None)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.GroupGlobalPermissions.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.GroupGlobalPermissions.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.GroupId, model.GroupId)
                    .SetProperty(p => p.Kind, model.Kind)
                    .SetProperty(p => p.Allow, model.Allow));
            }
            else
            {
                await ctx.GroupGlobalPermissions.AddAsync(model);
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

    private static IQueryable<GroupPermissionGlobalModel> GetQueryable(
        ApplicationDbContext db,
        GroupPermissionGlobalInclude include)
    {
        IQueryable<GroupPermissionGlobalModel> q = db.GroupGlobalPermissions.AsNoTracking();
        if (include.HasFlag(GroupPermissionGlobalInclude.Group))
        {
            q = q.Include(e => e.Group);
        }
        return q;
    }
}

[Flags]
public enum GroupPermissionGlobalInclude : byte
{
    None = 0,

    Group = 1 << 0
}