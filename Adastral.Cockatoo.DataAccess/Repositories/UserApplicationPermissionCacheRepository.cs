using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class UserApplicationPermissionCacheRepository(ApplicationDbContext db)
{
    public Task<List<UserApplicationPermissionCacheModel>> GetForUser(Guid userId)
    {
        return db.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }
    public Task<List<UserApplicationPermissionCacheModel>> GetForUserAndApp(Guid userId, Guid appId)
    {
        return db.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == userId && e.ApplicationId == appId)
            .ToListAsync();
    }
    public Task<List<ScopedApplicationPermissionKind>> GetPermissionsForUserAndApp(Guid userId, Guid appId)
    {
        return db.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == userId && e.ApplicationId == appId)
            .Select(e => e.Permission)
            .Distinct()
            .ToListAsync();
    }
    private async Task TransactionWrapper(
        IEnumerable<UserApplicationPermissionCacheModel> models,
        TransactionWrapperCallback logic)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await Task.WhenAll(models
                .Select(e => new
                {
                    Key = new GroupingKey
                    {
                        UserId = e.UserId,
                        ApplicationId = e.ApplicationId
                    },
                    Value = e,
                })
                .GroupBy(e => e.Key, e => e.Value)
                .Select(e => logic(ctx, e)));

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public Task Add(params IEnumerable<UserApplicationPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformAdd);
    }
    public Task Remove(params IEnumerable<UserApplicationPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformRemove);
    }
    public Task Set(params IEnumerable<UserApplicationPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformSet);
    }

    private sealed class GroupingKey
    {
        public required Guid UserId { get; set; }
        public required Guid ApplicationId { get; set; }
    }
    private delegate Task TransactionWrapperCallback(ApplicationDbContext ctx, IGrouping<GroupingKey, UserApplicationPermissionCacheModel> group);

    private static async Task PerformAdd(
        ApplicationDbContext ctx,
        IGrouping<GroupingKey, UserApplicationPermissionCacheModel> group)
    {
        var existing = await ctx.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == group.Key.UserId && e.ApplicationId == group.Key.ApplicationId)
            .Select(e => e.Permission)
            .ToListAsync();

        var permissions = group.Select(e => e.Permission)
            .Distinct()
            .ToList();

        await ctx.UserApplicationPermissionCache.AddRangeAsync(permissions
            .Where(e => !existing.Contains(e))
            .Select(permission => new UserApplicationPermissionCacheModel
            {
                UserId = group.Key.UserId,
                ApplicationId = group.Key.ApplicationId,
                Permission = permission
            }));
    }
    private static async Task PerformRemove(
        ApplicationDbContext ctx,
        IGrouping<GroupingKey, UserApplicationPermissionCacheModel> group)
    {
        var existing = await ctx.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == group.Key.UserId && e.ApplicationId == group.Key.ApplicationId)
            .Select(e => e.Permission)
            .ToListAsync();

        var permissions = group.Select(e => e.Permission)
            .Where(existing.Contains)
            .Distinct()
            .ToList();

        await ctx.UserApplicationPermissionCache
            .Where(e
                => e.UserId == group.Key.UserId
                && e.ApplicationId == group.Key.ApplicationId
                && permissions.Contains(e.Permission))
            .ExecuteDeleteAsync();
    }
    private static async Task PerformSet(
        ApplicationDbContext ctx,
        IGrouping<GroupingKey, UserApplicationPermissionCacheModel> group)
    {
        var existing = await ctx.UserApplicationPermissionCache.AsNoTracking()
            .Where(e => e.UserId == group.Key.UserId && e.ApplicationId == group.Key.ApplicationId)
            .Select(e => e.Permission)
            .ToListAsync();

        var permissions = group.Select(e => e.Permission).Distinct().ToList();

        await ctx.UserApplicationPermissionCache
            .Where(e
                => e.UserId == group.Key.UserId
                && e.ApplicationId == group.Key.ApplicationId
                && !permissions.Contains(e.Permission))
            .ExecuteDeleteAsync();

        await ctx.UserApplicationPermissionCache.AddRangeAsync(permissions
            .Where(e => !existing.Contains(e))
            .Select(permission => new UserApplicationPermissionCacheModel
            {
                UserId = group.Key.UserId,
                ApplicationId = group.Key.ApplicationId,
                Permission = permission
            }));
    }
}
