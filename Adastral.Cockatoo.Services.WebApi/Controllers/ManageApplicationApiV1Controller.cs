using System.ComponentModel.DataAnnotations;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[Route("~/api/v1/Manage/Application/")]
public class ManageApplicationApiV1Controller(IServiceProvider services) : Controller()
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationRepository _appDetailRepo = services.GetRequiredService<ApplicationRepository>();
    private readonly PermissionWebService _permissionWebService = services.GetRequiredService<PermissionWebService>();
    private readonly StorageService _storageService = services.GetRequiredService<StorageService>();
    private readonly StorageFileRepository _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
    private readonly AUDNRevisionRepository _audnRevisionRepo = services.GetRequiredService<AUDNRevisionRepository>();
    
    [HttpPost("{appId}/AutoUpdaterDotNet/SubmitRevision")]
    [ProducesResponseType(typeof(AutoUpdaterDotNetRevisionModel), 200, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 403, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.ApplicationDetailAUDNSubmitRevision)]
    public async Task<ActionResult> SubmitAUDNRevision(
        Guid appId,
        [Required] [FromQuery] string version,
        [Required] [FromQuery] string filename)
    {
        var app = await _appDetailRepo.GetById(appId);
        if (app == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationModel)} with Id {appId}",
                PropertyName = nameof(appId)
            }, BaseService.SerializerOptions);
        }
        
        bool includePrivate = await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationDetailViewAll);
        if (app!.IsPrivate && !includePrivate)
        {
            Response.StatusCode = 403;
            return Json(new NotAuthorizedResponse()
            {
                MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
            }, BaseService.SerializerOptions);
        }

        if (app.Type != ApplicationType.AutoUpdaterDotNet)
        {
            Response.StatusCode = 401;
            return Json(new ExceptionWebResponse(new ArgumentException($"Application {app.DisplayName} ({app.Id}) has invalid type {app.Type}, must be {ApplicationType.AutoUpdaterDotNet}.")), BaseService.SerializerOptions);
        }
        if (string.IsNullOrEmpty(version))
        {
            Response.StatusCode = 401;
            return Json(new ExceptionWebResponse(new ArgumentException($"Parameter is required", nameof(version))), BaseService.SerializerOptions);
        }
        if (string.IsNullOrEmpty(filename))
        {
            Response.StatusCode = 401;
            return Json(new ExceptionWebResponse(new ArgumentException($"Parameter is required", nameof(filename))), BaseService.SerializerOptions);
        }

        if (Request.ContentLength == null || Request.ContentLength == 0)
        {
            Response.StatusCode = 401;
            return Json(new ExceptionWebResponse(new Exception($"Request header \"content-length\" is required for uploading files.")), BaseService.SerializerOptions);
        }

        var file = await _storageService.UploadFile(Request.Body, filename, Request.ContentLength);
        if (string.IsNullOrEmpty(file.ContentType))
        {
            file.ContentType = MimeTypes.GetMimeType(filename);
            file = await _storageFileRepo.InsertOrUpdate(file);
        }

        try
        {
            var model = new AutoUpdaterDotNetRevisionModel()
            {
                ApplicationId = app.Id,
                Version = version,
                StorageFileId = file.Id,
            };
            model = await _audnRevisionRepo.InsertOrUpdate(model);

            Response.StatusCode = 200;
            return Json(model, BaseService.SerializerOptions);
        }
        catch
        {
            try
            {
                await _storageService.Delete(file);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete file {file.Id} after failed to insert new {nameof(AutoUpdaterDotNetRevisionModel)}");
                SentrySdk.CaptureException(new AggregateException($"Failed to delete file {file.Id} after failed to insert new {nameof(AutoUpdaterDotNetRevisionModel)}", ex));
            }
            throw;
        }
    }
}