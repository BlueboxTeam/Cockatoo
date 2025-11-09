using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class GroupUserAssociationRepository(ApplicationDbContext db)
{
    public Task<GroupMembershipModel?> GetById(
        Guid associationId,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        return GetQueryable(db, include)
            .Where(e => includeDeleted || !e.IsDeleted)
            .Where(e => e.Id == associationId)
            .FirstOrDefaultAsync();
    }

    public Task<List<GroupMembershipModel>> GetAllForGroup(
        Guid groupId,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        return GetQueryable(db, include)
            .Where(e => includeDeleted || !e.IsDeleted)
            .Where(e => e.GroupId == groupId)
            .ToListAsync();
    }
    public Task<List<GroupMembershipModel>> GetAllForUser(
        Guid userId,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        return GetQueryable(db, include)
            .Where(e => includeDeleted || !e.IsDeleted)
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }
    public Task<List<GroupMembershipModel>> GetAllWithGroupAndUser(
        GroupModel group,
        UserModel user,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        return GetAllWithUserAndGroup(user.Id, group.Id, includeDeleted, include);
    }
    public Task<List<GroupMembershipModel>> GetAllWithUserAndGroup(
        Guid userId,
        Guid groupId,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        return GetQueryable(db, include)
            .Where(e => includeDeleted || !e.IsDeleted)
            .Where(e => e.GroupId == groupId && e.UserId == userId)
            .ToListAsync();
    }

    public Task<int> SoftDeleteById(params Guid[] ids) => SetDeleteState(true, ids);
    public async Task<int> SetDeleteState(bool state, params Guid[] ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.GroupMemberships
                .Where(e => ids.Contains(e.Id))
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsDeleted, state));

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
    public async Task<int> HardDeleteById(params Guid[] ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.GroupMemberships
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
    public async Task<List<GroupMembershipModel>> HardDeleteByGroupId(params Guid[] groupIds)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var models = await ctx.GroupMemberships.AsNoTracking()
                .Where(e => groupIds.Contains(e.GroupId))
                .ToListAsync();
            await ctx.GroupMemberships
                .Where(e => groupIds.Contains(e.GroupId))
                .ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return models;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public Task<bool> ExistsById(Guid id)
    {
        return db.GroupMemberships.AnyAsync(e => e.Id == id);
    }
    public Task<bool> ExistsByGroupAndUser(
        GroupModel group,
        UserModel user,
        bool includeDeleted = false,
        GroupMembershipInclude include = GroupMembershipInclude.None)
        => ExistsByGroupAndUser(group.Id, user.Id, includeDeleted);
    public Task<bool> ExistsByGroupAndUser(
        Guid groupId,
        Guid userId,
        bool includeDeleted = false)
    {
        return db.GroupMemberships.AsNoTracking()
            .Where(e => includeDeleted || !e.IsDeleted)
            .Where(e => e.GroupId == groupId && e.UserId == userId)
            .AnyAsync();
    }
    public async Task<GroupMembershipModel> InsertOrUpdate(
        GroupMembershipModel model,
        GroupMembershipInclude include = GroupMembershipInclude.None)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.GroupMemberships.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.GroupMemberships.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.UserId, model.UserId)
                    .SetProperty(p => p.GroupId, model.GroupId)
                    .SetProperty(p => p.IsDeleted, model.IsDeleted));
            }
            else
            {
                await ctx.GroupMemberships.AddAsync(model);
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

    private static IQueryable<GroupMembershipModel> GetQueryable(
        ApplicationDbContext db,
        GroupMembershipInclude include)
    {
        IQueryable<GroupMembershipModel> q = db.GroupMemberships.AsNoTracking();
        if (include.HasFlag(GroupMembershipInclude.Group))
        {
            q = q.Include(e => e.Group);
        }
        if (include.HasFlag(GroupMembershipInclude.User))
        {
            q = q.Include(e => e.User);
        }
        if (include.HasFlag(GroupMembershipInclude.CreatedByUser))
        {
            q = q.Include(e => e.CreatedByUser);
        }
        return q;
    }
}

[Flags]
public enum GroupMembershipInclude : byte
{
    None = 0,

    Group = 1 << 0,
    User = 1 << 1,
    CreatedByUser = 1 << 2
}