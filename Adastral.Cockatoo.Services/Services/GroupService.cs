using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class GroupService
{
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermAppRepo;
    private readonly PermissionCacheService _permissionCacheService;
    private readonly ApplicationDbContext _db;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public GroupService(IServiceProvider services)
    {
        _db = services.GetRequiredService<ApplicationDbContext>();
        _userRepo = services.GetRequiredService<UserRepository>();
        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
    }

    #region Get Users In
    /// <summary>
    /// Get a list of all user models in the <paramref name="groupId"/> specified.
    /// </summary>
    /// <remarks>
    /// Calls <see cref="GetUsersInAsync(GroupModel)"/> when the group could be found in <see cref="GroupRepository"/>
    /// </remarks>
    public async Task<List<UserModel>> GetUsersInAsync(Guid groupId)
    {
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }

        return await GetUsersInAsync(group);
    }

    /// <summary>
    /// Get a list of all user models in the <paramref name="group"/> specified.
    /// </summary>
    public async Task<List<UserModel>> GetUsersInAsync(GroupModel group)
    {
        var associations = await _groupUserAssocRepo.GetAllForGroup(group.Id);
        var result = new List<UserModel>();

        foreach (var item in associations)
        {
            var user = await _userRepo.GetById(item.UserId);
            if (user != null)
            {
                result.Add(user);
            }
        }

        return result;
    }
    #endregion

    #region Add User
    /// <summary>
    /// Add a User to a Group.
    /// </summary>
    /// <param name="groupId">Id of the <see cref="GroupModel"/></param>
    /// <param name="userId">Id of the <see cref="UserModel"/></param>
    /// <remarks>
    /// Fetches the <see cref="GroupModel"/> and <see cref="UserModel"/>, then calls <see cref="AddUserAsync(GroupModel, UserModel)"/>
    /// </remarks>
    public async Task AddUserAsync(Guid groupId, Guid userId)
    {
        var groupModel = await _groupRepo.GetById(groupId);
        var userModel = await _userRepo.GetById(userId);

        if (groupModel == null || userModel == null)
        {
            throw new AggregateException(
                new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId)),
                new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}", nameof(userId))
            );
        }
        if (groupModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }
        if (userModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}", nameof(userId));
        }

        await AddUserAsync(groupModel, userModel);
    }

    /// <summary>
    /// Add a <paramref name="user"/> to the <paramref name="group"/> provided.
    /// </summary>
    /// <param name="group">Group to add the user into.</param>
    /// <param name="user">User to be added into the group</param>
    /// <remarks>
    /// <para>Nothing will be done if <see cref="GroupUserAssociationRepository.ExistsByGroupAndUser(GroupModel, UserModel)"/> returns <see langword="true"/></para>
    ///
    /// <para>Uses <see cref="SentrySdk.CaptureException(Exception, Action{Scope})"/></para>
    /// </remarks>
    public async Task AddUserAsync(GroupModel group, UserModel user)
    {
        var associationExists = await _groupUserAssocRepo.ExistsByGroupAndUser(group, user);
        if (associationExists)
        {
            return;
        }

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.GroupMemberships
                .Where(e
                    => e.GroupId == group.Id
                    && e.UserId == user.Id
                    && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsDeleted, true));
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra("group", group);
                scope.SetExtra("user", user);
                scope.SetTag("param.group.Id", group.Id.ToString());
                scope.SetTag("param.user.Id", user.Id.ToString());
            });
            throw;
        }
    }
    #endregion

    #region Add Multiple Users
    /// <summary>
    /// Add many users by their IDs to the group provided.
    /// </summary>
    /// <remarks>
    /// Fetches <see cref="GroupModel"/> by the <paramref name="groupId"/> provided, tries to get all instances of <see cref="UserModel"/>
    /// where the ID matches (for all items in <paramref name="userIds"/>), then calls <see cref="AddManyUsersAsync(GroupModel, IEnumerable{UserModel})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(Guid groupId, params IEnumerable<Guid> userIds)
    {
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }
        await AddManyUsersAsync(group, userIds);
    }
    /// <summary>
    /// Add many users by their IDs to the <paramref name="group"/> provided.
    /// </summary>
    /// <remarks>
    /// Fetches all users in <paramref name="userIds"/>, then passes that through to <see cref="AddManyUsersAsync(GroupModel, IEnumerable{UserModel})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(GroupModel group, params IEnumerable<Guid> userIds)
    {
        var userList = new List<UserModel>();
        foreach (var id in userIds.Distinct())
        {
            try
            {
                var user = await _userRepo.GetById(id);
                if (user != null) userList.Add(user);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to get {nameof(UserModel)} with Id {id}");
            }
        }
        if (userList.Count < 1)
        {
            throw new ArgumentException($"No valid users found", nameof(userIds));
        }

        await AddManyUsersAsync(group, userList);
    }
    /// <summary>
    /// Add many <paramref name="users"/> to the <paramref name="group"/> provided (if they're not in it already)
    /// </summary>
    /// <param name="group">Group to add the users into.</param>
    /// <param name="users">Users to add into the group.</param>
    /// <remarks>
    /// Uses <see cref="SentrySdk.CaptureException(Exception, Action{Scope})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(GroupModel group, IEnumerable<UserModel> users)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {

            await ctx.GroupMemberships.AddRangeAsync(users.Select(user => new GroupMembershipModel
            {
                GroupId = group.Id,
                UserId = user.Id
            }));
            

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra($"param.{nameof(users)}.Id", users.Select(v => v.Id).ToArray());
                scope.SetExtra($"param.{nameof(group)}.Id", group.Id);
            });
            throw;
        }
    }
    #endregion

    #region Remove User
    /// <summary>
    /// Remove a user from a group.
    /// </summary>
    /// <param name="groupId">Id of the <see cref="GroupModel"/></param>
    /// <param name="userId">Id of the <see cref="UserModel"/></param>
    /// <remarks>
    /// Fetches the <see cref="GroupModel"/> and <see cref="UserModel"/>, then calls <see cref="RemoveUserAsync(GroupModel, UserModel)"/>
    /// </remarks>
    public async Task RemoveUserAsync(Guid groupId, Guid userId)
    {
        var groupModel = await _groupRepo.GetById(groupId);
        var userModel = await _userRepo.GetById(userId);

        if (groupModel == null || userModel == null)
        {
            throw new AggregateException(
                new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId)),
                new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}", nameof(userId))
            );
        }
        if (groupModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }
        if (userModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}", nameof(userId));
        }

        await RemoveUserAsync(groupModel, userModel);
    }
    /// <summary>
    /// Remove a <paramref name="user"/> from the <paramref name="group"/> specified.
    /// </summary>
    /// <remarks>
    /// When any documents exist in <see cref="GroupUserAssociationRepository"/> where the user & group matches, and
    /// <see cref="GroupUserAssociationModel.IsDeleted"/> is set to false, then it will be set to true.
    /// Otherwise, if any documents don't exist in <see cref="GroupUserAssociationRepository"/> where the user & group
    /// matches, then nothing will be done.
    /// </remarks>
    public async Task RemoveUserAsync(GroupModel group, UserModel user)
    {
        var associationExists = await _groupUserAssocRepo.ExistsByGroupAndUser(group, user, false);
        if (!associationExists)
            return;

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.GroupMemberships
                .Where(e
                    => e.GroupId == group.Id
                    && e.UserId == user.Id
                    && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.IsDeleted, true));
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra("group", group);
                scope.SetExtra("user", user);
                scope.SetTag("param.group.Id", group.Id.ToString());
                scope.SetTag("param.user.Id", user.Id.ToString());
            });
            throw;
        }
    }
    #endregion

    public class DeleteGroupResult
    {
        public GroupModel? Group { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GroupPermissionGlobalModel>? GlobalPermissions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GroupPermissionApplicationModel>? ApplicationPermissions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<UserModel>? AffectedUsers { get; set; }
    }
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };
    public async Task<DeleteGroupResult> DeleteGroupAsync(GroupModel group)
    {
        // var session = await _mongoClient.StartSessionAsync();
        var result = new DeleteGroupResult()
        {
            Group = group
        };
        async Task Revert()
        {
            if (result.Group != null)
            {
                if (!await _groupRepo.Exists(result.Group.Id))
                {
                    try
                    {
                        await _groupRepo.InsertOrUpdate(result.Group);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Revert|Failed to re-insert {nameof(GroupModel)}\n{JsonSerializer.Serialize(result.Group, SerializerOptions)}");
                    }
                }
            }
            if (result.AffectedUsers?.Count > 0)
            {
                foreach (var usr in result.AffectedUsers)
                {
                    var model = new GroupMembershipModel
                    {
                        UserId = usr.Id,
                        GroupId = group.Id,
                        IsDeleted = false
                    };
                    try
                    {
                        await _groupUserAssocRepo.InsertOrUpdate(model);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Revert|Failed to re-insert {nameof(GroupMembershipModel)}\n{JsonSerializer.Serialize(model, SerializerOptions)}");
                    }
                }
            }
            if (result.GlobalPermissions?.Count > 0)
            {
                foreach (var glb in result.GlobalPermissions)
                {
                    try
                    {
                        await _groupPermGlobalRepo.InsertOrUpdate(glb);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Revert|Failed to re-insert {nameof(GroupPermissionGlobalModel)}\n{JsonSerializer.Serialize(glb, SerializerOptions)}");
                    }
                }
            }
            if (result.ApplicationPermissions?.Count > 0)
            {
                foreach (var app in result.ApplicationPermissions)
                {
                    try
                    {
                        await _groupPermAppRepo.InsertOrUpdate(app);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Revert|Failed to re-insert {nameof(GroupPermissionApplicationModel)}\n{JsonSerializer.Serialize(app, SerializerOptions)}");
                    }
                }
            }
            if (result.AffectedUsers?.Count > 0)
            {
                foreach (var usr in result.AffectedUsers)
                {
                    try
                    {
                        await _permissionCacheService.CalculateUser(usr.Id);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Revert|Failed to recalculate user permissions for {usr} ({usr.Id})");
                    }
                }
            }
        }

        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            result.AffectedUsers = await ctx.GroupMemberships.Where(e => e.GroupId == group.Id && !e.IsDeleted).Select(e => e.User).AsNoTracking().ToListAsync();

            result.GlobalPermissions = await ctx.GroupGlobalPermissions.AsNoTracking()
                .Where(e => e.GroupId == group.Id)
                .ToListAsync();
            result.ApplicationPermissions = await ctx.GroupApplicationPermissions.AsNoTracking()
                .Where(e => e.GroupId == group.Id)
                .ToListAsync();

            await ctx.GroupGlobalPermissions.Where(e => e.GroupId == group.Id).ExecuteDeleteAsync();
            await ctx.GroupApplicationPermissions.Where(e => e.GroupId == group.Id).ExecuteDeleteAsync();
            await ctx.GroupMemberships.Where(e => e.GroupId == group.Id).ExecuteDeleteAsync();
            await ctx.Groups.Where(e => e.Id == group.Id).ExecuteDeleteAsync();

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra($"param.{nameof(group)}.Id", group.Id.ToString());
            });
            throw;
        }

        _log.Debug($"Recalculating permissions for {result.AffectedUsers.Count} users.");
        foreach (var user in result.AffectedUsers)
        {
            await _permissionCacheService.CalculateUser(user.Id);
        }
        return result;
    }
}