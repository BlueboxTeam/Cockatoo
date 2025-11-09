using Adastral.Cockatoo.DataAccess.Models;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

public class GroupPermissionApplicationRepository(ApplicationDbContext db)
{
    public Task<GroupPermissionApplicationModel?> GetById(
        Guid id,
        GroupApplicationPermissionsInclude include = GroupApplicationPermissionsInclude.None)
    {
        return GetQueryable(db, include)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<List<GroupPermissionApplicationModel>> GetAll(GroupApplicationPermissionsInclude include = GroupApplicationPermissionsInclude.None)
    {
        return GetQueryable(db, include)
            .ToListAsync();
    }

    public Task<List<GroupPermissionApplicationModel>> GetManyByGroup(
        Guid groupId,
        GroupApplicationPermissionsInclude include = GroupApplicationPermissionsInclude.None)
    {
        return GetQueryable(db, include)
            .Where(e => e.GroupId == groupId)
            .ToListAsync();
    }

    public Task<List<GroupPermissionApplicationModel>> GetManyByApplication(
        Guid applicationId)
        => GetManyByApplication(applicationId, GroupApplicationPermissionsInclude.None);
    public Task<List<GroupPermissionApplicationModel>> GetManyByApplication(
        Guid applicationId,
        GroupApplicationPermissionsInclude include)
    {
        return GetQueryable(db, include)
            .Where(e => e.ApplicationId == applicationId)
            .ToListAsync();
    }

    public class GetManyByOptions
    {
        public Maybe<Guid> Id { get; init; }
        public Maybe<Guid> GroupId { get; init; }
        public Maybe<Guid> ApplicationId { get; init; }
        public Maybe<ScopedApplicationPermissionKind[]> KindsIn { get; init; }
        public Maybe<ScopedApplicationPermissionKind[]> KindsEq { get; init; }
        public Maybe<bool> Allow { get; init; }
        public Maybe<GroupApplicationPermissionsInclude> Include { get; set; }
    }
    public Task<List<GroupPermissionApplicationModel>> GetManyBy(GetManyByOptions options)
    {
        var query = GetQueryable(db, options.Include.GetValueOrDefault(GroupApplicationPermissionsInclude.None));

        if (options.Id.HasValue)
        {
            query = query.Where(e => e.Id == options.Id.Value);
        }
        if (options.GroupId.HasValue)
        {
            query = query.Where(e => e.GroupId == options.GroupId.Value);
        }
        if (options.ApplicationId.HasValue)
        {
            query = query.Where(e => e.ApplicationId == options.ApplicationId.Value);
        }


        if (options.KindsIn.HasValue)
        {
            var @in = options.KindsIn.Value;
            query = query.Where(e => @in.Contains(e.Kind));
        }
        if (options.KindsEq.HasValue)
        {
            foreach (var kind in options.KindsEq.Value)
            {
                query = query.Where(e => e.Kind == kind);
            }
        }

        if (options.Allow.HasValue)
        {
            query = query.Where(e => e.Allow == options.Allow.Value);
        }
        return query.ToListAsync();
    }

    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.GroupApplicationPermissions
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

    public async Task<GroupPermissionApplicationModel> InsertOrUpdate(
        GroupPermissionApplicationModel model,
        GroupApplicationPermissionsInclude include = GroupApplicationPermissionsInclude.None)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.GroupApplicationPermissions.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.GroupApplicationPermissions.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.GroupId, model.GroupId)
                    .SetProperty(p => p.ApplicationId, model.ApplicationId)
                    .SetProperty(p => p.Kind, model.Kind)
                    .SetProperty(p => p.Allow, model.Allow));
            }
            else
            {
                await ctx.GroupApplicationPermissions.AddAsync(model);
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

    private static IQueryable<GroupPermissionApplicationModel> GetQueryable(
        ApplicationDbContext db,
        GroupApplicationPermissionsInclude include)
    {
        IQueryable<GroupPermissionApplicationModel> q = db.GroupApplicationPermissions.AsNoTracking();
        if (include.HasFlag(GroupApplicationPermissionsInclude.Group))
        {
            q = q.Include(e => e.Group);
        }
        if (include.HasFlag(GroupApplicationPermissionsInclude.Application))
        {
            q = q.Include(e => e.Application);
        }
        return q;
    }
}

[Flags]
public enum GroupApplicationPermissionsInclude : byte
{
    None = 0,

    Group = 1 << 0,
    Application = 1 << 1
}