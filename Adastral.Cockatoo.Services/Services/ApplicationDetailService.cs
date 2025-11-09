using System.Buffers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class ApplicationDetailService : BaseService
{
    private readonly ApplicationRepository _appDetailRepo;
    private readonly StorageService _storageService;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly AUDNRevisionRepository _audnRevisionRepo;
    private readonly IDistributedCache _cache;
    private readonly MongoClient _mongoClient;
    private readonly BullseyeService _bullseyeService;
    private readonly GroupPermissionApplicationRepository _groupPermissionAppRepo;
    private readonly PermissionCacheService _permissionCacheService;
    private readonly CockatooConfig _config;
    public ApplicationDetailService(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<CockatooConfig>();
        _appDetailRepo = services.GetRequiredService<ApplicationRepository>();
        _storageService = services.GetRequiredService<StorageService>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _audnRevisionRepo = services.GetRequiredService<AUDNRevisionRepository>();
        _mongoClient = services.GetRequiredService<MongoClient>();
        _bullseyeService = services.GetRequiredService<BullseyeService>();
        _groupPermissionAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
        _cache = services.GetRequiredService<IDistributedCache>();
    }

    internal class AUDNXMLCacheKey
    {
        #region Constructors
        public AUDNXMLCacheKey()
            : this(Guid.Empty)
        {
        }

        public AUDNXMLCacheKey(Guid appId, bool includeDisabled = false)
        {
            AppId = appId;
            IncludeDisabled = includeDisabled;
        }
        #endregion

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Type => CockatooHelper.FormatTypeName(GetType());

        public Guid AppId { get; set; }

        [DefaultValue((false))]
        public bool IncludeDisabled { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, BaseService.SerializerOptions);
        }
    }

    private string? GetAUDNXMLCache(Guid appId, bool includeDisabled = false)
    {
        var key = new AUDNXMLCacheKey(appId, includeDisabled);
        var keyJson = key.ToJson();
        var data = _cache.GetString(keyJson);
        if (data == null)
            return null;

        return data;
    }

    public async Task<string?> GetAUDNXML(Guid appId, bool includeDisabled = false, bool force = false)
    {
        var xmlString = GetAUDNXMLCache(appId, includeDisabled);
        if (xmlString != null && !force)
        {
            return xmlString;
        }
        return await GenerateAUDNXML(appId, includeDisabled);
    }
    public async Task<string?> GenerateAUDNXML(Guid appId, bool includeDisabled = false)
    {
        var app = await _appDetailRepo.GetById(appId);
        if (app == null)
        {
            throw new ArgumentException($"Could not find Application with Id {appId}", nameof(appId));
        }
        if (app.Type != ApplicationType.AutoUpdaterDotNet)
        {
            throw new ValidationException($"Application {app.Id} type is invalid, must be {ApplicationType.AutoUpdaterDotNet} but it's {app.Type}");
        }

        var latest = await _audnRevisionRepo.GetLatestForApp(app.Id, includeDisabled);
        if (latest == null)
        {
            return null;
        }
        var fileModel = await _storageFileRepo.GetById(latest.StorageFileId);
        if (fileModel == null)
        {
            throw new NoNullAllowedException($"Failed to get File with Id {latest.StorageFileId} for AUDN Revision {latest.Id} (app: {latest.ApplicationId})");
        }

        var data = new UpdateInfoEventArgs()
        {
            DownloadUrl = $"{_config.PublicUrl}/api/v1/ApplicationDetail/Id/{app.Id}/AutoUpdateDotNet/File",
            CurrentVersion = latest.Version,
            ChangelogUrl = latest.ChangelogUrl,
            ExecutablePath = latest.ExecutablePath,
            InstallerArgs = latest.ExecutableLaunchArguments,
            Mandatory = new()
            {
                Value = latest.Mandatory,
                MinimumVersion = string.IsNullOrEmpty(latest.MandatoryMinimumVersion)
                    ? null
                    : latest.MandatoryMinimumVersion,
                UpdateMode = latest.MandatoryKind
            }
        };
        if (fileModel.HasHash())
        {
            data.CheckSum = new()
            {
                Value = fileModel.Sha256Hash,
                HashingAlgorithm = "SHA256"
            };
        }

        var key = new AUDNXMLCacheKey(appId, includeDisabled);
        await _cache.SetStringAsync(key.ToJson(), data.Serialize());
        return data.Serialize();
    }

    /// <summary>
    /// Get the XML content for the latest revision in <see cref="AUDNRevisionRepository"/>.
    /// </summary>
    /// <returns>Will return <see langword="null"/> when there is no latest version.</returns>
    public async Task<string?> GenerateAUDNXMLManual(Guid appId, bool includeDisabled = false)
    {
        var app = await _appDetailRepo.GetById(appId);
        if (app == null)
        {
            throw new ArgumentException($"Could not find Application with Id {appId}", nameof(appId));
        }
        if (app.Type != ApplicationType.AutoUpdaterDotNet)
        {
            throw new ValidationException($"Application {app.Id} type is invalid, must be {ApplicationType.AutoUpdaterDotNet} but it's {app.Type}");
        }

        var latest = await _audnRevisionRepo.GetLatestForApp(app.Id, includeDisabled);
        if (latest == null)
        {
            return null;
        }
        var fileModel = await _storageFileRepo.GetById(latest.StorageFileId);
        if (fileModel == null)
        {
            throw new NoNullAllowedException($"Failed to get File with Id {latest.StorageFileId} for AUDN Revision {latest.Id} (app: {latest.ApplicationId})");
        }
        var resultLines = new List<string>()
        {
            $"<version>{latest.Version}</version>",
            $"<url>{_config.PublicUrl}/api/v1/ApplicationDetail/Id/{app.Id}/AutoUpdateDotNet/File</url>",
        };
        if (!string.IsNullOrEmpty(latest.ChangelogUrl))
        {
            resultLines.Add($"<changelog>{latest.ChangelogUrl}</changelog>");
        }
        if (latest.Mandatory)
        {
            var attrs = new List<string>();
            if (latest.MandatoryKind != AutoUpdaterDotNetMandatoryKind.Normal)
            {
                attrs.Add($"mode=\"{(int)latest.MandatoryKind}\"");
            }
            if (!string.IsNullOrEmpty(latest.MandatoryMinimumVersion) && latest.MandatoryMinimumVersion != "0.0.0.0")
            {
                attrs.Add($"minVersion=\"{latest.MandatoryMinimumVersion.Trim()}\"");
            }
            var attrsJoin = string.Join(" ", attrs);
            resultLines.Add($"<mandatory {attrsJoin}>{latest.Mandatory}</mandatory>");
        }
        if (!string.IsNullOrEmpty(latest.ExecutablePath))
        {
            resultLines.Add($"<executable>{latest.ExecutablePath}</executable>");
        }
        if (!string.IsNullOrEmpty(latest.ExecutableLaunchArguments))
        {
            resultLines.Add($"<args>{latest.ExecutableLaunchArguments.Trim()}</args>");
        }
        if (fileModel.HasHash())
        {
            resultLines.Add($"<checksum algorithm=\"SHA256\">{fileModel.Sha256Hash}</checksum>");
        }

        resultLines = [
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<item>",
            ..resultLines,
            "</item>"
        ];
        return string.Join("\n", resultLines);
    }

    public async Task Delete(Guid appId, UserModel? deletedByUser = null)
    {
        // TODO refactor to soft delete instead of hard delete
        var model = await _appDetailRepo.GetById(appId);
        if (model == null)
        {
            throw new ArgumentException(
                $"Could not find {nameof(ApplicationModel)} with Id {appId}", nameof(appId));
        }

        using (var session = await _mongoClient.StartSessionAsync())
        {
            session.StartTransaction();

            try
            {
                await _bullseyeService.DeleteBullseyeApp(model.Id, true);
                var appPermissions = await _groupPermissionAppRepo.GetManyByApplication(model.Id);
                await _groupPermissionAppRepo.Delete(appPermissions.Select(v => v.Id).ToArray());
                await _appDetailRepo.DeleteById(deletedByUser, model.Id);
                
                foreach (var x in appPermissions)
                {
                    await _permissionCacheService.CalculateGroup(x.GroupId);
                }
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw new InvalidOperationException($"Failed to delete Application {model.DisplayName} ({model.Id})", ex);
            }

            await session.CommitTransactionAsync();
        }
    }
}