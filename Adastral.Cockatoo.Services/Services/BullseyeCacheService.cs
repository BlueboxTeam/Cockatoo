using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class BullseyeCacheService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly BullseyeAppRepository _bullAppRepo;
    private readonly BullseyeRevisionRepository _bullAppRevRepo;
    private readonly BullseyePatchRepository _bullPatchRepo;
    private readonly ApplicationRepository _appDetailRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    private readonly BullseyeV1CacheRepository _bullV1CacheRepo;
    private readonly BullseyeV2CacheRepository _bullV2CacheRepo;
    public BullseyeCacheService(IServiceProvider services)
        : base(services)
    {
        _bullAppRepo = services.GetRequiredService<BullseyeAppRepository>();
        _bullAppRevRepo = services.GetRequiredService<BullseyeRevisionRepository>();
        _bullPatchRepo = services.GetRequiredService<BullseyePatchRepository>();

        _appDetailRepo = services.GetRequiredService<ApplicationRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();

        _bullV1CacheRepo = services.GetRequiredService<BullseyeV1CacheRepository>();
        _bullV2CacheRepo = services.GetRequiredService<BullseyeV2CacheRepository>();
    }

    /// <summary>
    /// Result for <see cref="GenerateCache"/>
    /// </summary>
    public class GenerateCacheResult
    {
        /// <summary>
        /// Generated Cache Model for <see cref="BullseyeV1"/>
        /// </summary>
        public BullseyeV1CacheModel V1 { get; set; } = new();
        /// <summary>
        /// Generated Cache Model for <see cref="BullseyeV2"/>
        /// </summary>
        public BullseyeV2CacheModel V2 { get; set; } = new();
        /// <summary>
        /// Was a new record added in <see cref="BullseyeAppRepository"/>
        /// </summary>
        public bool IsNewBullseyeApp { get; set; }
    }
    /// <summary>
    /// Generate cache
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <param name="publishedOnly">When set to <see langword="true"/>, then only revisions that are live will be used.</param>
    /// <param name="setLiveState">When not <see langword="null"/>, the IsLive value for <see cref="BullseyeV1CacheModel"/>/<see cref="BullseyeV2CacheModel"/> will be set to it.</param>
    public async Task<GenerateCacheResult> GenerateCache(Guid appId, bool publishedOnly, bool? setLiveState = null)
    {
        var appDetail = await _appDetailRepo.GetById(appId);
        if (appDetail == null)
        {
            throw new ArgumentException($"Could not find Application with Id {appId}", nameof(appId));
        }
        var bullApp = await _bullAppRepo.GetById(appId);
        bool isNewApp = bullApp == null;
        bullApp ??= new();
        bullApp.ApplicationId = appId;
        if (isNewApp)
        {
            await _bullAppRepo.InsertOrUpdate(bullApp);
        }

        var v1 = new BullseyeV1();
        var v2 = new BullseyeV2()
        {
            Name = appDetail.SourceMod.FolderName,
            SchemaVersion = 2
        };
        v2.SetLastUpdated();
        var revisions = await _bullAppRevRepo.GetAllForApp(appId, publishedOnly ? true : null);
        var revisionDict = revisions.ToDictionary(v => v.Id, v => v);
        BullseyeRevisionModel? highestVersion = null;
        var toPatchIds = new List<(Guid, string)>();
        foreach (var item in revisions.OrderBy(v => v.Version))
        {
            var v1VersionInfo = new BullseyeV1VersionInfo();
            var v2VersionInfo = new BullseyeV2VersionInfo();
            if (!string.IsNullOrEmpty(item.Tag))
            {
                v2VersionInfo.Tag = item.Tag;
            }

            StorageFileModel? archiveFile = null;
            if (item.ArchiveStorageFileId.HasValue)
            {
                archiveFile = await _storageFileRepo.GetById(item.ArchiveStorageFileId.Value);
            }
            if (item.SignatureStorageFileId.HasValue)
            {
                var signatureFile = await _storageFileRepo.GetById(item.SignatureStorageFileId.Value);
                if (signatureFile != null)
                {
                    v1VersionInfo.SignatureUrl = _storageService.GetUrl(signatureFile);
                    v2VersionInfo.SignatureFilename = v1VersionInfo.SignatureUrl;
                }
            }

            if (item.PeerToPeerStorageFileId.HasValue)
            {
                var torrentFile = await _storageFileRepo.GetById(item.PeerToPeerStorageFileId.Value);
                if (torrentFile != null)
                {
                    v1VersionInfo.TorrentUrl = _storageService.GetUrl(torrentFile);
                    v2VersionInfo.TorrentFilename = v1VersionInfo.TorrentUrl;
                }
            }
            if (archiveFile != null)
            {
                v1VersionInfo.Filename = _storageService.GetUrl(archiveFile);
                v2VersionInfo.Filename = _storageService.GetUrl(archiveFile);
                v2VersionInfo.FileSize = archiveFile.Size;
            }
            v2VersionInfo.ExtractedSize = item.Size;


            if (item.PreviousRevisionId.HasValue)
            {
                var previous = revisions.FirstOrDefault(v => v.Id == item.PreviousRevisionId);
                if (previous != null)
                {
                    v2VersionInfo.PreviousVersionKey = previous.Version.ToString();
                }
            }

            if (highestVersion == null || highestVersion.Version < item.Version)
            {
                highestVersion = item;
            }

            var versionId = item.Version.ToString();
            v1.Versions[versionId] = v1VersionInfo;
            v2.Versions[versionId] = v2VersionInfo;
            v1.Patches[versionId] = new();
            v2.Patches[versionId] = new();
            toPatchIds.Add((item.Id, versionId));
        }
        if (bullApp.LatestRevisionId.HasValue)
        {
            var latest = await _bullAppRevRepo.GetById(bullApp.LatestRevisionId.Value);
            if (latest != null)
            {
                highestVersion = latest;
            }
            else
            {
                _log.Warn($"Couldn't find latest revision {bullApp.LatestRevisionId}");
            }
        }
        foreach (var (toPatchId, targetVersionId) in toPatchIds)
        {
            var upgradeSources = await _bullPatchRepo.GetAllRevisionTo(toPatchId);
            foreach (var source in upgradeSources)
            {
                var v1Item = new BullseyeV1PatchInfo();
                var v2Item = new BullseyeV2PatchInfo();
                if (!revisionDict.TryGetValue(source.FromRevisionId, out var sourceRevision))
                {
                    continue;
                }

                var patchFile = await _storageFileRepo.GetById(source.StorageFileId);
                v1Item.TorrentUrl = null;
                v2Item.TorrentFilename = null;
                if (source.PeerToPeerStorageFileId.HasValue)
                {
                    var torrentFile = await _storageFileRepo.GetById(source.PeerToPeerStorageFileId.Value);
                    if (torrentFile != null)
                    {
                        v1Item.TorrentUrl = _storageService.GetUrl(torrentFile);
                        v2Item.TorrentFilename = _storageService.GetUrl(torrentFile);
                    }
                }
                if (patchFile != null)
                {
                    v1Item.Filename = _storageService.GetUrl(patchFile);
                    v1Item.TemporarySpaceRequired = patchFile.Size ?? 0;
                    v2Item.Filename = v1Item.Filename;
                    v2Item.FileSize = v1Item.TemporarySpaceRequired;
                    v2Item.TemporarySpaceRequired = v2Item.FileSize * 2;
                }

                if (toPatchId == highestVersion?.Id)
                {
                    v1.Patches[sourceRevision.Version.ToString()] = v1Item;
                }
                v2.Patches[targetVersionId][sourceRevision.Version.ToString()] = v2Item;
            }
        }

        if (highestVersion != null)
        {
            v2.LatestVersion = highestVersion.Version.ToString();
        }

        var v1Cache = new BullseyeV1CacheModel()
        {
            IsLive = setLiveState == null
                ? publishedOnly
                : (bool)setLiveState,
            ApplicationId = appId,
            Content = v1
        };
        var v2Cache = new BullseyeV2CacheModel()
        {
            IsLive = v1Cache.IsLive,
            ApplicationId = appId,
            Content = v2
        };

        await _bullV1CacheRepo.InsertOrUpdate(v1Cache);
        await _bullV2CacheRepo.InsertOrUpdate(v2Cache);
        return new()
        {
            V1 = v1Cache,
            V2 = v2Cache,
            IsNewBullseyeApp = isNewApp
        };
    }

    private static readonly Mutex GetLatestV1GenerateMutex = new();
    public async Task<BullseyeV1> GetLatestV1(Guid appId, bool? liveState = null)
    {
        var cacheModel = await _bullV1CacheRepo.GetByAppId(appId, liveState);
        if (cacheModel == null)
        {
            GetLatestV1GenerateMutex.WaitOne();
            // generate new cache, and include non-live patches when liveState isn't null and it's false
            var res = await GenerateCache(appId, liveState == null || (bool)liveState, liveState);
            GetLatestV1GenerateMutex.ReleaseMutex();
            return res.V1.Content;
        }
        return cacheModel.Content;
    }
    private static readonly Mutex GetLatestV2GenerateMutex = new();
    public async Task<BullseyeV2> GetLatestV2(Guid appId, bool? liveState = null)
    {
        var cacheModel = await _bullV2CacheRepo.GetByAppId(appId, liveState);
        if (cacheModel == null)
        {
            GetLatestV2GenerateMutex.WaitOne();
            // generate new cache, and include non-live patches when liveState isn't null and it's false
            var res = await GenerateCache(appId, liveState == null || (bool)liveState, liveState);
            GetLatestV2GenerateMutex.ReleaseMutex();
            return res.V2.Content;
        }
        return cacheModel.Content;
    }
}