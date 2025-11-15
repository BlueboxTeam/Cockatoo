using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class UserGlobalPermissionCacheRepository(ApplicationDbContext db)
{
    public Task<List<UserGlobalPermissionCacheModel>> GetForUser(Guid userId)
    {
        return db.UserGlobalPermissionCache.AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();
    }

    private async Task TransactionWrapper(
        IEnumerable<UserGlobalPermissionCacheModel> models,
        TransactionWrapperCallback logic)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await Task.WhenAll(models
                .GroupBy(e => e.UserId)
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

    public Task Add(params IEnumerable<UserGlobalPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformAdd);
    }
    public Task Remove(params IEnumerable<UserGlobalPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformRemove);
    }
    public Task Set(params IEnumerable<UserGlobalPermissionCacheModel> models)
    {
        return TransactionWrapper(models, PerformSet);
    }

    private delegate Task TransactionWrapperCallback(ApplicationDbContext ctx, IGrouping<Guid, UserGlobalPermissionCacheModel> group);

    private static async Task PerformAdd(
        ApplicationDbContext ctx,
        IGrouping<Guid, UserGlobalPermissionCacheModel> group)
    {
        var existing = await ctx.UserGlobalPermissionCache.AsNoTracking()
            .Where(e => e.UserId == group.Key)
            .Select(e => e.Permission)
            .ToListAsync();
        var permissions = group.Select(e => e.Permission).Distinct().ToList();

        await ctx.UserGlobalPermissionCache.AddRangeAsync(permissions
            .Where(e => !existing.Contains(e))
            .Select(permission => new UserGlobalPermissionCacheModel
            {
                UserId = group.Key,
                Permission = permission
            }));
    }
    private static async Task PerformRemove(
        ApplicationDbContext ctx,
        IGrouping<Guid, UserGlobalPermissionCacheModel> group)
    {
        var permissions = group.Select(e => e.Permission).Distinct().ToList();

        await ctx.UserGlobalPermissionCache
            .Where(e => e.UserId == group.Key && permissions.Contains(e.Permission))
            .ExecuteDeleteAsync();
    }
    private static async Task PerformSet(
        ApplicationDbContext ctx,
        IGrouping<Guid, UserGlobalPermissionCacheModel> group)
    {
        var existing = await ctx.UserGlobalPermissionCache.AsNoTracking()
            .Where(e => e.UserId == group.Key)
            .Select(e => e.Permission)
            .ToListAsync();
        var permissions = group.Select(e => e.Permission).Distinct().ToList();

        await ctx.UserGlobalPermissionCache
            .Where(e => e.UserId == group.Key && !permissions.Contains(e.Permission))
            .ExecuteDeleteAsync();

        await ctx.UserGlobalPermissionCache.AddRangeAsync(permissions
            .Where(e => !existing.Contains(e))
            .Select(permission => new UserGlobalPermissionCacheModel
            {
                UserId = group.Key,
                Permission = permission
            }));
    }
}
