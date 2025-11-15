using Adastral.Cockatoo.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Data;
using System.Text.Json;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Adastral.Cockatoo.DataAccess;

namespace Adastral.Cockatoo.Services;

public class BullseyeService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly BullseyeV1CacheRepository _bullCacheV1Repo;
    private readonly BullseyeV2CacheRepository _bullCacheV2Repo;
    private readonly BullseyeAppRepository _bullseyeAppRepo;
    private readonly BullseyeRevisionRepository _bullRevisionRepo;
    private readonly BullseyePatchRepository _bullseyePatchRepo;
    private readonly BullseyeCacheService _bullseyeCacheService;
    private readonly ApplicationRepository _appDetailRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    private readonly ApplicationDbContext _db;
    public BullseyeService(IServiceProvider services)
        : base(services)
    {
        _bullCacheV1Repo = services.GetRequiredService<BullseyeV1CacheRepository>();
        _bullCacheV2Repo = services.GetRequiredService<BullseyeV2CacheRepository>();
        _bullseyeAppRepo = services.GetRequiredService<BullseyeAppRepository>();
        _bullRevisionRepo = services.GetRequiredService<BullseyeRevisionRepository>();
        _bullseyePatchRepo = services.GetRequiredService<BullseyePatchRepository>();
        _bullseyeCacheService = services.GetRequiredService<BullseyeCacheService>();
        _appDetailRepo = services.GetRequiredService<ApplicationRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();
        _db = services.GetRequiredService<ApplicationDbContext>();
    }
    
    public async Task<List<ApplicationModel>> GetAllApps()
    {
        var apps = (await _appDetailRepo.GetAll())
            .Where(v => v.Type == ApplicationType.Kachemak)
            .ToList();
        foreach (var item in apps)
        {
            if (!await _bullseyeAppRepo.Exists(item.Id))
            {
                await _bullseyeAppRepo.InsertOrUpdate(new()
                {
                    ApplicationId = item.Id
                });
            }
        }
        return apps;
    }

    public class GetBullseyeAppResult
    {
        /// <summary>
        /// Generated (or found) instance of <see cref="BullseyeAppModel"/>. When <see langword="null"/>, act as if an
        /// exception was thrown.
        /// </summary>
        public ApplicationBullseyeModel? App { get; set; } = null;
        /// <summary>
        /// Was a new instance of <see cref="BullseyeAppModel"/> created? Only set to <see langword="true"/> when one
        /// doesn't exist in <see cref="BullseyeAppRepository"/> and <see cref="AppDetailExists"/> is set to <see langword="true"/>
        /// </summary>
        public bool CreatedNewApp { get; set; } = false;
        /// <summary>
        /// Does a document in <see cref="ApplicationDetailRepository"/> exist where <see cref="ApplicationDetailModel.Id"/>
        /// equals the <c>id</c> provided.
        /// </summary>
        public bool AppDetailExists { get; set; } = false;
        /// <summary>
        /// Only set when <see cref="AppDetailExists"/> is set to <see langword="true"/>. When <see langword="false"/>,
        /// then consider it a failure.
        /// </summary>
        public bool? AppDetailTypeAllowed { get; set; } = null;
    }
    public async Task<GetBullseyeAppResult> GetApp(Guid id)
    {
        var appDetail = await _appDetailRepo.GetById(id);
        if (appDetail == null)
        {
            return new();
        }

        if (appDetail.Type != ApplicationType.Kachemak)
        {
            return new()
            {
                AppDetailExists = true,
                AppDetailTypeAllowed = false
            };
        }

        var bullApp = await _bullseyeAppRepo.GetById(id);
        var result = new GetBullseyeAppResult()
        {
            CreatedNewApp = bullApp == null,
            AppDetailExists = true,
            AppDetailTypeAllowed = true
        };
        if (bullApp == null)
        {
            bullApp = new()
            {
                ApplicationId = id
            };
            await _bullseyeAppRepo.InsertOrUpdate(bullApp);
        }

        result.App = bullApp;
        return result;
    }

    public enum CanRegisterVersionResult
    {
        /// <summary>
        /// Revision can be registered!
        /// </summary>
        Success = 0,
        /// <summary>
        /// Could not find an instance of <see cref="ApplicationDetailModel"/> with the Id provided.
        /// </summary>
        AppDetailNotFound,
        /// <summary>
        /// The value for <see cref="ApplicationDetailModel.Type"/> on the instance found is not equal to
        /// <see cref="ApplicationDetailType.Kachemak"/>
        /// </summary>
        AppDetailInvalidType,
        /// <summary>
        /// A revision for the app provided already exists with the version provided.
        /// </summary>
        VersionAlreadyExists,
        /// <summary>
        /// A revision for the app provided already exists with the tag provided.
        /// </summary>
        TagAlreadyExists,
        /// <summary>
        /// Could not find an instance of <see cref="StorageFileModel"/> with the Id provided.
        /// </summary>
        ArchiveFileNotFound,
        /// <summary>
        /// When the <c>peerToPeerFileId</c> parameter in <see cref="CanRegisterRevision"/> is set, and
        /// the instance of <see cref="StorageFileModel"/> could not be found, then this is returned.
        /// </summary>
        PeerToPeerFileNotFound,
        /// <summary>
        /// When the <c>signatureFileId</c> parameter in <see cref="CanRegisterRevision"/> is set, and
        /// the instance of <see cref="StorageFileModel"/> could not be found, then this is returned.
        /// </summary>
        SignatureFileNotFound,
        /// <summary>
        /// When the <c>previousRevisionId</c> parameter in <see cref="CanRegisterRevision"/> is set and the instance
        /// of <see cref="BullseyeAppRevisionModel"/> couldn't be found, then this is returned.
        /// </summary>
        PreviousRevisionNotFound,
        /// <summary>
        /// When the Application Id associated with the previous revision doesn't match the App Id provided 
        /// in <see cref="CanRegisterRevision"/>, then this is returned.
        /// </summary>
        PreviousRevisionAppMismatch
    }
    public async Task<CanRegisterVersionResult> CanRegisterRevision(
        Guid appId,
        int version,
        string? tag,
        Guid archiveFileId,
        Guid? peerToPeerFileId = null,
        Guid? signatureFileId = null,
        Guid? previousRevisionId = null)
    {
        var appDetailModel = await _appDetailRepo.GetById(appId);
        if (appDetailModel == null)
        {
            return CanRegisterVersionResult.AppDetailNotFound;
        }
        if (appDetailModel.Type != ApplicationType.Kachemak)
        {
            return CanRegisterVersionResult.AppDetailInvalidType;
        }

        if (await _bullRevisionRepo.ExistsByApplicationAndVersion(appId, version))
        {
            return CanRegisterVersionResult.VersionAlreadyExists;
        }

        if (!string.IsNullOrEmpty(tag) && await _bullRevisionRepo.ExistsByApplicationAndTag(appId, tag))
        {
            return CanRegisterVersionResult.TagAlreadyExists;
        }

        var archiveFile = await _storageFileRepo.GetById(archiveFileId);
        if (archiveFile == null)
        {
            return CanRegisterVersionResult.ArchiveFileNotFound;
        }

        if (peerToPeerFileId.HasValue)
        {
            var peerFile = await _storageFileRepo.GetById(peerToPeerFileId.Value);
            if (peerFile == null)
            {
                return CanRegisterVersionResult.PeerToPeerFileNotFound;
            }
        }

        if (signatureFileId.HasValue)
        {
            var signatureFile = await _storageFileRepo.GetById(signatureFileId.Value);
            if (signatureFile == null)
            {
                return CanRegisterVersionResult.SignatureFileNotFound;
            }
        }

        if (previousRevisionId.HasValue)
        {
            var previousRevision = await _bullRevisionRepo.GetById(previousRevisionId.Value);
            if (previousRevision == null)
            {
                return CanRegisterVersionResult.PreviousRevisionNotFound;
            }
            if (previousRevision.ApplicationId != appId)
            {
                return CanRegisterVersionResult.PreviousRevisionAppMismatch;
            }
        }

        return CanRegisterVersionResult.Success;
    }
    
    /// <summary>
    /// Delete a Bullseye App
    /// </summary>
    /// <param name="appId">Bullseye Application Id (<see cref="BullseyeAppModel.ApplicationDetailModelId"/>)</param>
    /// <param name="deleteResources"><inheritdoc cref="ManageBullseyeV1DeleteRequest.DeleteStorageResources" path="/summary"/></param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="appId"/> is null or empty.</exception>
    public Task<ManageBullseyeV1DeleteResponse> DeleteBullseyeApp(Guid appId, bool deleteResources, bool createTransaction = true)
    {
        if (appId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(appId)} is required", nameof(appId));
        }
        return DeleteBullseyeApp(new()
        {
            AppId = appId,
            IncludeResources = deleteResources
        }, createTransaction: createTransaction);
    }
    private async Task<ManageBullseyeV1DeleteResponse> DeleteBullseyeAppInternal(ManageBullseyeV1DeleteRequest req)
    {
        if (req.AppId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(req.AppId)} is required", nameof(req));
        }
        var response = new ManageBullseyeV1DeleteResponse()
        {
            Request = req
        };

        try
        {
            response.ApplicationDetailModel = await _appDetailRepo.GetById(req.AppId);
        }
        catch (Exception ex)
        {
            response.ApplicationDetailModelException = new(ex);
        }
        try
        {
            response.BullseyeAppModel = await _bullseyeAppRepo.GetById(req.AppId);
            if (response.BullseyeAppModel != null)
            {
                await _bullseyeAppRepo.Delete(response.BullseyeAppModel.ApplicationId);
            }
        }
        catch (Exception ex)
        {
            response.BullseyeAppModelException = new(ex);
        }

        // find & mark revisions/files for deletion.
        foreach (var revision in await _bullRevisionRepo.GetAllForApp(req.AppId))
        {
            try
            {
                response.DeletedRevisions.Add(revision);

                // Keep track of associated StorageFileModels that will be
                // deleted if the requester wants that.
                if (req.IncludeResources)
                {
                    if (revision.ArchiveStorageFileId.HasValue)
                    {
                        var archiveFile = await _storageFileRepo.GetById(revision.ArchiveStorageFileId.Value);
                        if (archiveFile != null)
                        {
                            response.DeletedFiles.Add(archiveFile);
                        }
                    }
                    if (revision.PeerToPeerStorageFileId.HasValue)
                    {
                        var p2pFile = await _storageFileRepo.GetById(revision.PeerToPeerStorageFileId.Value);
                        if (p2pFile != null)
                        {
                            response.DeletedFiles.Add(p2pFile);
                        }
                    }
                    if (revision.SignatureStorageFileId.HasValue)
                    {
                        var signatureFile = await _storageFileRepo.GetById(revision.SignatureStorageFileId.Value);
                        if (signatureFile != null)
                        {
                            response.DeletedFiles.Add(signatureFile);
                        }
                    }
                }

                await _bullRevisionRepo.Delete(revision.Id);
            }
            catch (Exception ex)
            {
                response.DeleteRevisionExceptions[revision.Id] = new(ex);
            }
        }

        #region Bullseye Cache Models
        // v1
        try
        {
            response.LatestV1CacheModel = await _bullCacheV1Repo.GetByAppId(req.AppId, false);
        }
        catch (Exception ex)
        {
            response.LatestV1CacheModelException = new(ex);
        }
        try
        {
            response.LatestLiveV1CacheModel = await _bullCacheV1Repo.GetByAppId(req.AppId, true);
        }
        catch (Exception ex)
        {
            response.LatestLiveV1CacheModelException = new(ex);
        }
        // delete
        try
        {
            await _bullCacheV1Repo.DeleteForAppId(req.AppId);
        }
        catch (Exception ex)
        {
            response.DeleteV1CacheModelException = new(ex);
        }

        // v2
        try
        {
            response.LatestV2CacheModel = await _bullCacheV2Repo.GetByAppId(req.AppId, false);
        }
        catch (Exception ex)
        {
            response.LatestV2CacheModelException = new(ex);
        }
        try
        {
            response.LatestLiveV2CacheModel = await _bullCacheV2Repo.GetByAppId(req.AppId, true);
        }
        catch (Exception ex)
        {
            response.LatestLiveV2CacheModelException = new(ex);
        }
        // delete
        try
        {
            await _bullCacheV2Repo.DeleteForAppId(req.AppId);
        }
        catch (Exception ex)
        {
            response.DeleteV2CacheModelException = new(ex);
        }
        #endregion


        // delete all files that were marked for deletion.
        foreach (var file in response.DeletedFiles)
        {
            try
            {
                await _storageService.Delete(file);
            }
            catch (Exception ex)
            {
                response.DeleteFileExceptions[file.Id] = new(ex);
            }
        }

        return response;
    }
    /// <summary>
    /// Delete a Bullseye App and it's resources (when <see cref="ManageBullseyeV1DeleteRequest.IncludeResources"/> is <see langword="true"/>)
    /// </summary>
    /// <param name="req">Request Options.</param>
    /// <exception cref="ArgumentException">Thrown when <see cref="ManageBullseyeV1DeleteRequest.AppId"/> is null or empty.</exception>
    public async Task<ManageBullseyeV1DeleteResponse> DeleteBullseyeApp(ManageBullseyeV1DeleteRequest req, bool createTransaction = true)
    {
        if (!createTransaction)
        {
            return await DeleteBullseyeAppInternal(req);
        }
        await using var trans = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await DeleteBullseyeAppInternal(req);
            await _db.SaveChangesAsync();
            await trans.CommitAsync();
            return result;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<ManageBullseyeV1DeleteRevisionResponse> DeleteBullseyeRevision(Guid revisionId)
    {
        // TODO fix error logging
        if (revisionId == Guid.Empty)
        {
            throw new ArgumentException($"Cannot be empty ({Guid.Empty})", nameof(revisionId));
        }
        var response = new ManageBullseyeV1DeleteRevisionResponse()
        {
            RequestRevisionId = revisionId
        };

        BullseyeRevisionModel? revisionModel = null;
        try
        {
            revisionModel = await _bullRevisionRepo.GetById(revisionId);
            if (revisionModel == null)
            {
                throw new NoNullAllowedException($"{nameof(_bullRevisionRepo)}.{nameof(_bullRevisionRepo.GetById)} returned null");
            }
            await _bullRevisionRepo.Delete(revisionModel.Id);
        }
        catch (Exception ex)
        {
            response.DeletedRevisionException = new(ex);
            return response;
        }

        ApplicationBullseyeModel? appModel = null;
        try
        {
            appModel = await _bullseyeAppRepo.GetById(revisionModel.ApplicationId);
            if (appModel == null)
            {
                throw new NoNullAllowedException($"{nameof(_bullseyeAppRepo)}.{nameof(_bullseyeAppRepo.GetById)} returned null");
            }
            var before = JsonSerializer.Deserialize<ApplicationBullseyeModel>(JsonSerializer.Serialize(appModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            if (appModel.LatestRevisionId == revisionModel.Id)
            {
                appModel.LatestRevisionId = null;
                appModel = await _bullseyeAppRepo.InsertOrUpdate(appModel);
                response.BullseyeAppComparison = new(before, appModel);
            }
        }
        catch (Exception ex)
        {
            response.BullseyeAppComparisonException = new(ex);
        }

        if (revisionModel.ArchiveStorageFileId.HasValue)
        {
            try
            {
                var fileModel = await _storageFileRepo.GetById(revisionModel.ArchiveStorageFileId.Value);
                if (fileModel != null)
                {
                    response.DeletedFiles.Add(fileModel);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"{nameof(revisionId)}={revisionId}|delete={nameof(revisionModel.ArchiveStorageFileId)} {revisionModel.ArchiveStorageFileId}");
            }
        }
        if (revisionModel.PeerToPeerStorageFileId.HasValue)
        {
            try
            {
                var fileModel = await _storageFileRepo.GetById(revisionModel.PeerToPeerStorageFileId.Value);
                if (fileModel != null)
                {
                    response.DeletedFiles.Add(fileModel);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"{nameof(revisionId)}={revisionId}|delete={nameof(revisionModel.PeerToPeerStorageFileId)} {revisionModel.PeerToPeerStorageFileId}");
            }
        }
        if (revisionModel.SignatureStorageFileId.HasValue)
        {
            try
            {
                var fileModel = await _storageFileRepo.GetById(revisionModel.SignatureStorageFileId.Value);
                if (fileModel != null)
                {
                    response.DeletedFiles.Add(fileModel);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"{nameof(revisionId)}={revisionId}|delete={nameof(revisionModel.SignatureStorageFileId)} {revisionModel.SignatureStorageFileId}");
            }
        }

        foreach (var patch in await _bullseyePatchRepo.GetAllWithRevision(revisionModel.Id))
        {
            try
            {
                var file = await _storageFileRepo.GetById(patch.StorageFileId);
                if (file != null)
                {
                    response.DeletedFiles.Add(file);
                }

                if (patch.PeerToPeerStorageFileId.HasValue)
                {
                    var p2pFile = await _storageFileRepo.GetById(patch.PeerToPeerStorageFileId.Value);
                    if (p2pFile != null)
                    {
                        response.DeletedFiles.Add(p2pFile);
                    }
                }
                await _bullseyePatchRepo.Delete(patch.Id);
            }
            catch (Exception ex)
            {
                response.DeletePatchExceptions[patch.Id] = new(ex);
            }
        }


        try
        {
            if (appModel != null)
            {
                await _bullseyeCacheService.GenerateCache(appModel.ApplicationId, true, true);
                await _bullseyeCacheService.GenerateCache(appModel.ApplicationId, false, false);
            }
        }
        catch (Exception ex)
        {
            response.GenerateCacheException = new(ex);
        }


        // delete all files that were marked for deletion.
        foreach (var file in response.DeletedFiles)
        {
            try
            {
                await _storageService.Delete(file);
            }
            catch (Exception ex)
            {
                response.DeleteFileExceptions[file.Id] = new(ex);
            }
        }

        return response;
    }

    public async Task<ManageBullseyeV1DeletePatchResponse> DeletePatch(Guid patchId)
    {
        ManageBullseyeV1DeletePatchResponse result = new()
        {
            RequestPatchId = patchId
        };
        try
        {
            result.DeletedPatch = await _bullseyePatchRepo.GetById(patchId);
            if (result.DeletedPatch == null)
            {
                throw new NoNullAllowedException($"{nameof(_bullseyePatchRepo)}.{nameof(_bullseyePatchRepo.GetById)} returned null");
            }
        }
        catch (Exception ex)
        {
            result.DeletedPatchException = new(ex);
            result.Success = false;
            return result;
        }

        if (result.DeletedPatch?.StorageFileId != null)
        {
            try
            {
                var file = await _storageFileRepo.GetById(result.DeletedPatch!.StorageFileId);
                if (file != null)
                {
                    await _storageService.Delete(file);
                    result.DeletedFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                result.DeleteFileExceptions ??= [];
                result.DeleteFileExceptions[result.DeletedPatch!.StorageFileId] = new(ex);
            }
        }
        if (result.DeletedPatch?.PeerToPeerStorageFileId.HasValue ?? false)
        {
            try
            {
                var file = await _storageFileRepo.GetById(result.DeletedPatch!.PeerToPeerStorageFileId.Value);
                if (file != null)
                {
                    await _storageService.Delete(file);
                    result.DeletedFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                result.DeleteFileExceptions ??= [];
                result.DeleteFileExceptions[result.DeletedPatch!.StorageFileId] = new(ex);
            }
        }
        result.Success = result.DeleteFileExceptions?.Count < 1;
        return result;
    }
}