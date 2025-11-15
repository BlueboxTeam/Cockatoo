using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services;

public class UserService
{
    private readonly PermissionService _permissionService;
    private readonly ApplicationDbContext _db;

    public UserService(IServiceProvider services)
    {
        _permissionService = services.GetRequiredService<PermissionService>();
        _db = services.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<UserModel> CreateServiceAccount(UserModel owner, string? name = null)
    {
        if (owner.IsServiceAccount)
        {
            throw new ArgumentException($"Service Accounts cannot create Service Accounts");
        }
        var userModel = new UserModel()
        {
            Email = null,
            IsServiceAccount = true
        };
        var saModel = new ServiceAccountModel()
        {
            OwnerUserId = owner.Id,
            UserId = userModel.Id
        };
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {

            await ctx.Users.AddAsync(userModel);
            await ctx.ServiceAccounts.AddAsync(saModel);
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
        return userModel;
    }

    public async Task<ServiceAccountTokenModel> CreateToken(UserModel target, DateTimeOffset? expiresAt)
    {
        if (!target.IsServiceAccount)
        {
            throw new ArgumentException($"Provided user is not a Service Account", nameof(target));
        }
        var model = new ServiceAccountTokenModel()
        {
            ServiceAccountId = target.Id,
            ExpiresAt = expiresAt
        };
        await using var ctx = _db.CreateSession();
        await ctx.ServiceAccountTokens.AddAsync(model);
        await ctx.SaveChangesAsync();
        return await ctx.ServiceAccountTokens.AsNoTracking()
            .SingleAsync(e => e.Id == model.Id);
    }

    public async Task<CanUserCreateTokenKind> CanCreateTokenFor(UserModel requestingUser, UserModel targetUser)
    {
        if (!targetUser.IsServiceAccount)
        {
            return CanUserCreateTokenKind.TargetUserIsNotServiceAccount;
        }
        if (requestingUser.IsServiceAccount)
        {
            return CanUserCreateTokenKind.RequestingUserIsServiceAccount;
        }
        var serviceAccountModel = await _db.ServiceAccounts.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == targetUser.Id);
        if (serviceAccountModel == null)
        {
            return CanUserCreateTokenKind.TargetUserIsNotServiceAccount;
        }
        if (serviceAccountModel.OwnerUserId != requestingUser.Id)
        {
            var check = await _permissionService.CheckGlobalPermission(
                requestingUser,
                PermissionService.PermissionFilterType.Any, 
                PermissionKind.ServiceAccountAdmin);
            if (!check)
            {
                return CanUserCreateTokenKind.RequestingUserIsNotOwner;
            }
        }
        return CanUserCreateTokenKind.Yes;
    }
}