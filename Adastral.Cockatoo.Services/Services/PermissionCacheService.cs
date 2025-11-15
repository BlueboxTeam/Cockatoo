using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry.Protocol;
using System.Buffers;
using System.Reflection;
using System.Text.Json;

namespace Adastral.Cockatoo.Services;

public class PermissionCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
    };

    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermAppRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;

    private readonly ApplicationRepository _appRepo;
    private readonly UserGlobalPermissionCacheRepository _userGlobalPermissionCache;
    private readonly UserApplicationPermissionCacheRepository _userApplicationPermissionCache;
    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _distCache;

    public PermissionCacheService(IServiceProvider services)
    {
        _userRepo = services.GetRequiredService<UserRepository>();

        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();

        _userGlobalPermissionCache = services.GetRequiredService<UserGlobalPermissionCacheRepository>();
        _userApplicationPermissionCache = services.GetRequiredService<UserApplicationPermissionCacheRepository>();
        _appRepo = services.GetRequiredService<ApplicationRepository>();
        _db = services.GetRequiredService<ApplicationDbContext>();
        _distCache = services.GetRequiredService<IDistributedCache>();
    }

    public static List<PermissionKind> GetInheritedPermissions(PermissionKind kind)
    {
        return GetInheritedPermissionsInternal(kind, 0, []);
    }
    private static List<PermissionKind> GetInheritedPermissionsInternal(
        PermissionKind kind,
        uint depth,
        List<PermissionKind> permissionStack)
    {
        permissionStack.Add(kind);
        if (depth > 64)
        {
            throw new StackOverflowException($"Circular Reference when trying to get inherited permissions for {kind}.\npermissionStack: "
                                             + string.Join('.', permissionStack.Select(v => $"{v}")));
        }
        var result = new List<PermissionKind>();

        var enumType = typeof(PermissionKind);
        var memberInfos = enumType.GetMember(kind.ToString());
        var enumValueMemberInfo = memberInfos.FirstOrDefault(v => v.DeclaringType == enumType);
        var valueAttributes = enumValueMemberInfo?.GetCustomAttributes<PermissionInheritAttribute>(false);
        foreach (var attr in valueAttributes ?? [])
        {
            result.Add(attr.InheritFrom);
        }

        if (result.Count > 0)
        {
            uint di = depth + 1;
            result.AddRange(result.SelectMany(v => GetInheritedPermissionsInternal(v, di, [..permissionStack])).ToList());
        }

        return result.Distinct().ToList();
    }

    /// <summary>
    /// Get user global permissions
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    /// <returns>List of permissions the user has.</returns>
    public async Task<ICollection<PermissionKind>> GetUser(Guid userId)
    {
        var stringContent = await _distCache.GetStringAsync(GetGlobalUserKey(userId));
        if (string.IsNullOrEmpty(stringContent))
        {
            var res = await CalculateUser(userId);
            return res.GlobalCache;
        }
        else
        {
            return JsonSerializer.Deserialize<List<PermissionKind>>(stringContent!, SerializerOptions) ?? [];
        }
    }

    /// <summary>
    /// Get all permissions for a specific application.
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    /// <param name="applicationId">Id of <see cref="ApplicationDetailModel"/></param>
    /// <returns>List of application permissions this user has for the specified application.</returns>
    public async Task<ICollection<ScopedApplicationPermissionKind>> GetUserByApplication(Guid userId, Guid applicationId)
    {
        var userPermissions = await GetUser(userId);

        var stringContent = await _distCache.GetStringAsync(GetApplicationUserKey(userId, applicationId));
        if (string.IsNullOrEmpty(stringContent))
        {
            var res = await CalculateUser(userId);
            if (res.ApplicationCache.TryGetValue(applicationId, out var item))
            {
                if (userPermissions.Contains(PermissionKind.Superuser))
                {
                    return item.Concat([ScopedApplicationPermissionKind.Admin]).Distinct().ToList();
                }
                return item;
            }
            else
            {
                if (userPermissions.Contains(PermissionKind.Superuser))
                {
                    return [ScopedApplicationPermissionKind.Admin];
                }
                return [];
            }
        }
        else
        {
            var data = JsonSerializer.Deserialize<List<ScopedApplicationPermissionKind>>(stringContent!, SerializerOptions) ?? [];
            if (userPermissions.Contains(PermissionKind.Superuser))
            {
                return data.Concat([ScopedApplicationPermissionKind.Admin]).Distinct().ToList();
            }
            return data;
        }
    }

    /// <summary>
    /// Result for <see cref="CalculateUser"/>
    /// </summary>
    public class RecalculateUserResult
    {
        public required ICollection<PermissionKind> GlobalCache { get; set; }
        public Dictionary<Guid, ICollection<ScopedApplicationPermissionKind>> ApplicationCache { get; set; } = [];
    }

    /// <summary>
    /// Calculate permissions for the <paramref name="userId"/> provided.
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    public async Task<RecalculateUserResult> CalculateUser(Guid userId)
    {
        var groupAssociations = await _groupUserAssocRepo.GetAllForUser(userId);
        var groups = await _groupRepo.GetManyById(groupAssociations.Select(v => v.GroupId).ToArray());
        var globalPermissions = new Dictionary<PermissionKind, bool>();
        var applicationPermissions = new Dictionary<Guid, Dictionary<ScopedApplicationPermissionKind, bool>>();
        foreach (var app in await _appRepo.GetAll())
        {
            applicationPermissions[app.Id] = [];
        }
        foreach (var groupId in groups.OrderByDescending(v => v.Priority).Select(e => e.Id))
        {
            var groupGlobalPermissions = await _groupPermGlobalRepo.GetManyByGroup(groupId);
            foreach (var item in groupGlobalPermissions)
            {
                foreach (var k in GetInheritedPermissions(item.Kind))
                {
                    globalPermissions[k] = item.Allow;
                }
                globalPermissions[item.Kind] = item.Allow;
            }

            var groupApplicationPermissions = await _groupPermAppRepo.GetManyByGroup(groupId);
            // do stuff that has an ApplicationId first, then override stuff when it's not set.
            foreach (var item in groupApplicationPermissions.OrderBy(v => !v.ApplicationId.HasValue ? 1 : 0))
            {
                if (item.ApplicationId.HasValue)
                {
                    if (!applicationPermissions.ContainsKey(item.ApplicationId.Value))
                    {
                        applicationPermissions[item.ApplicationId.Value] = [];
                    }

                    applicationPermissions[item.ApplicationId.Value][item.Kind] = item.Allow;
                }
                else
                {
                    foreach (var i in applicationPermissions)
                    {
                        applicationPermissions[i.Key][item.Kind] = item.Allow;
                    }
                }
            }
        }

        await _userGlobalPermissionCache.Set(globalPermissions.Where(e => e.Value)
            .Select(e => new UserGlobalPermissionCacheModel
            {
                UserId = userId,
                Permission = e.Key
            }));
        var result = new RecalculateUserResult()
        {
            GlobalCache = await _db.UserGlobalPermissionCache.AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => e.Permission)
                .ToListAsync()
        };
        await SetCacheAsync(GetGlobalUserKey(userId), result.GlobalCache);

        await Task.WhenAll(applicationPermissions.Select(SetApplicationPermissionCache));
        return result;

        async Task SetApplicationPermissionCache(KeyValuePair<Guid, Dictionary<ScopedApplicationPermissionKind, bool>> pair)
        {
            await _userApplicationPermissionCache.Set(pair.Value
                .Where(e => e.Value)
                .Select(e => new UserApplicationPermissionCacheModel
                {
                    UserId = userId,
                    ApplicationId = pair.Key,
                    Permission = e.Key
                }));
            var records = await _userApplicationPermissionCache.GetPermissionsForUserAndApp(userId, pair.Key);
            lock (result.ApplicationCache)
            {
                result.ApplicationCache[pair.Key] = records;
            }
            await SetCacheAsync(GetApplicationUserKey(userId, pair.Key), records);
        }
    }

    private async Task SetCacheAsync<TValue>(string key, TValue value)
        where TValue : notnull
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        await _distCache.SetStringAsync(key, json);
    }

    /// <summary>
    /// Calculate permissions for the <paramref name="groupId"/> provided.
    /// </summary>
    /// <param name="groupId">Id of <see cref="GroupModel"/></param>
    public async Task CalculateGroup(Guid groupId)
    {
        var associations = await _groupUserAssocRepo.GetAllForGroup(groupId);
        foreach (var assoc in associations)
        {
            try
            {
                var userModel = await _userRepo.GetById(assoc.UserId);
                if (userModel != null)
                {
                    await CalculateUser(assoc.UserId);
                }
                else
                {
                    try
                    {
                        await _groupUserAssocRepo.HardDeleteById(assoc.Id);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"Failed to delete unreferenced Group->User association (since the user doesn't exist anymore)");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AggregateException($"Failed to process user {assoc.UserId} for group {groupId}", ex);
            }
        }
    }

    private static string GetGlobalUserKey(Guid userId)
    {
        return $"{nameof(PermissionCacheService)},global,{nameof(userId)}={userId}";
    }

    private static string GetApplicationUserKey(Guid userId, Guid appId)
    {
        return $"{nameof(PermissionCacheService)},application,{nameof(userId)}={userId},{nameof(appId)}={appId}";
    }

    // TODO convert method into hosted service
    /*public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _log.Debug($"Calculating permissons for all users.");
        foreach (var user in await _userRepo.GetAll())
        {
            try
            {
                await CalculateUser(user.Id);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Could not calculate permissions for user {user.Id}");
            }
        }
    }*/
}