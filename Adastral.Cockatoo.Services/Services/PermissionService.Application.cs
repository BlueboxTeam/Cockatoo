using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Adastral.Cockatoo.Services;

public partial class PermissionService
{
    public static List<ScopedApplicationPermissionKind> MapPermission(List<PermissionKind> permissions)
    {
        var mapped = new List<ScopedApplicationPermissionKind>();
        var mappingTable = new Dictionary<PermissionKind, ScopedApplicationPermissionKind>()
        {
            { PermissionKind.ApplicationDetailAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.ApplicationDetailViewAll, ScopedApplicationPermissionKind.ReadOnly },
            { PermissionKind.ApplicationDetailEditAppearance, ScopedApplicationPermissionKind.EditAppearance },
            { PermissionKind.ApplicationDetailEditDetails, ScopedApplicationPermissionKind.EditDetails },

            { PermissionKind.ApplicationDetailAUDNAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.ApplicationDetailAUDNView, ScopedApplicationPermissionKind.ReadOnly },
            { PermissionKind.ApplicationDetailAUDNDeleteRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.ApplicationDetailAUDNSubmitRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.ApplicationDetailAUDNToggleRevisionEnableState, ScopedApplicationPermissionKind.ManageRevisions },

            { PermissionKind.RefreshSouthbank, ScopedApplicationPermissionKind.UpdateCache },

            { PermissionKind.BullseyeAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.BullseyeGenerateCache, ScopedApplicationPermissionKind.UpdateCache },
            { PermissionKind.BullseyeDeletePatch, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeDeleteRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeRegisterPatch, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeRegisterRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeUpdatePreviousRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeAppMarkLatestRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeUpdateRevisionLiveState, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeViewPrivateModels, ScopedApplicationPermissionKind.ReadOnly },
        };
        var mappingTableMany = new Dictionary<PermissionKind, List<ScopedApplicationPermissionKind>>()
        {
            {
                PermissionKind.ApplicationDetailAUDNSubmitRevision,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
            {
                PermissionKind.BullseyeRegisterPatch,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
            {
                PermissionKind.BullseyeRegisterRevision,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
        };
        foreach (var x in permissions)
        {
            if (mappingTable.TryGetValue(x, out var i))
            {
                mapped.Add(i);
            }
            if (mappingTableMany.TryGetValue(x, out var p))
            {
                mapped.AddRange(p);
            }
        }

        return mapped.Distinct().ToList();
    }
    #region Check Permission
    public async Task<bool> CheckApplicationPermission(
        Guid userId,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> permissions)
    {
        var kinds = permissions.Distinct().ToArray();
        var userPermissions = await _permissionCacheService.GetUserByApplication(userId, applicationId);
        foreach (var x in kinds)
        {
            if (userPermissions.Contains(x))
                return true;
        }

        return false;
    }
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        ApplicationModel application,
        params IEnumerable<ScopedApplicationPermissionKind> permissions)
        => CheckApplicationPermission(user.Id, application.Id, permissions);
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> permissions)
        => CheckApplicationPermission(user.Id, applicationId, permissions);

    public async Task<bool> CheckApplicationPermission(
        Guid userId,
        Guid applicationId,
        params IEnumerable<PermissionKind> permissions)
    {
        // Allow when user has any of those global permissions and/or they're a superuser
        bool check = await CheckGlobalPermission(
            userId,
            PermissionFilterType.Any, 
            [.. permissions, PermissionKind.Superuser ]);
        if (check)
        {
            return true;
        }
        var mapped = MapPermission(permissions.Distinct().ToList());
        return await CheckApplicationPermission(userId, applicationId, mapped.ToArray());
    }
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        ApplicationModel application,
        params IEnumerable<PermissionKind> permissions)
    => CheckApplicationPermission(user.Id, application.Id, permissions);

    public Task<bool> CheckApplicationPermission(
        UserModel user,
        Guid applicationId,
        params IEnumerable<PermissionKind> permissions)
        => CheckApplicationPermission(user.Id, applicationId, permissions);
    #endregion
    
    #region Grant
    public async Task GrantManyApplicationForGroupAsync(
        Guid groupId,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var existingPermissions = await ctx.GroupApplicationPermissions
                .Where(e
                    => e.GroupId == groupId
                    && e.ApplicationId == applicationId
                    && kinds.Contains(e.Kind))
                .ToListAsync();

            await ctx.GroupApplicationPermissions
                .Where(e
                    => e.GroupId == groupId
                    && e.ApplicationId == applicationId
                    && kinds.Contains(e.Kind)
                    && !e.Allow)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Allow, true));

            foreach (var kind in kinds.Where(e => !existingPermissions.Any(x => x.Kind == e)))
            {
                await ctx.GroupApplicationPermissions.AddAsync(new GroupPermissionApplicationModel
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    Kind = kind,
                    Allow = true
                });
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId.ToString());
                scope.SetTag($"param.{nameof(applicationId)}", applicationId.ToString());
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            await trans.RollbackAsync();
            throw;
        }

        await _permissionCacheService.CalculateGroup(groupId);
    }
    public Task GrantManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationModel application,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
        => GrantManyApplicationForGroupAsync(group.Id, application.Id, kinds);

    public Task GrantManyApplicationForGroupAsync(
        GroupModel group,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds) =>
        GrantManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
    
    #region Deny
    public async Task DenyManyApplicationForGroupAsync(
        Guid groupId,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var existingPermissions = await ctx.GroupApplicationPermissions
                .Where(e
                    => e.GroupId == groupId
                    && e.ApplicationId == applicationId
                    && kinds.Contains(e.Kind))
                .ToListAsync();

            await ctx.GroupApplicationPermissions
                .Where(e
                    => e.GroupId == groupId
                    && e.ApplicationId == applicationId
                    && kinds.Contains(e.Kind)
                    && e.Allow)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Allow, false));

            foreach (var kind in kinds.Where(e => !existingPermissions.Any(x => x.Kind == e)))
            {
                await ctx.GroupApplicationPermissions.AddAsync(new GroupPermissionApplicationModel
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    Kind = kind,
                    Allow = false
                });
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId.ToString());
                scope.SetTag($"param.{nameof(applicationId)}", applicationId.ToString());
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            throw;
        }

        await _permissionCacheService.CalculateGroup(groupId);
    }
    public Task DenyManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationModel application,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
        => DenyManyApplicationForGroupAsync(group.Id, application.Id, kinds);

    public Task DenyManyApplicationForGroupAsync(
        GroupModel group,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds) =>
        DenyManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
    
    #region Revoke
    public async Task RevokeManyApplicationForGroupAsync(
        Guid groupId,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.GroupApplicationPermissions
                .Where(e
                    => e.GroupId == groupId
                    && e.ApplicationId == applicationId
                    && kinds.Contains(e.Kind))
                .ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId.ToString());
                scope.SetTag($"param.{nameof(applicationId)}", applicationId.ToString());
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            await trans.RollbackAsync();
            throw;
        }
        await _permissionCacheService.CalculateGroup(groupId);
    }

    public Task RevokeManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationModel application,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
        => RevokeManyApplicationForGroupAsync(group.Id, application.Id, kinds);
    
    public Task RevokeManyApplicationForGroupAsync(
        GroupModel group,
        Guid applicationId,
        params IEnumerable<ScopedApplicationPermissionKind> kinds)
        => RevokeManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
}