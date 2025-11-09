using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BullseyeRevisionRepository(ApplicationDbContext db)
{
    public Task<BullseyeRevisionModel?> GetById(Guid id)
    {
        return db.BullseyeRevisions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    public Task<BullseyeRevisionModel?> GetByTagForApp(Guid appId, string? tag)
    {
        tag = tag?.Trim().ToLower().ReplaceLineEndings("");
        return db.BullseyeRevisions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationId == appId && e.Tag == tag && e.Tag != null);
    }
    public Task<List<BullseyeRevisionModel>> GetAllForApp(Guid appId, bool? includeLive = null)
    {
        var q = db.BullseyeRevisions.AsNoTracking()
            .Where(e => e.ApplicationId == appId);
        if (includeLive.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            if (includeLive.Value)
            {
                q = q.Where(e
                    => e.ApplicationId == appId
                    && (e.IsLive || (e.PublishAt != null && e.PublishAt < now)));
            }
            else
            {
                q = q.Where(e
                    => e.ApplicationId == appId
                    && (!e.IsLive || (e.PublishAt != null && e.PublishAt > now)));
            }
        }
        else
        {
            q = q.Where(e => e.ApplicationId == appId);
        }
        return q.ToListAsync();
    }
    public Task<List<BullseyeRevisionModel>> GetAll()
    {
        return db.BullseyeRevisions.AsNoTracking().ToListAsync();
    }

    public Task<List<BullseyeRevisionModel>> GetAllForAppWithVersion(Guid appId, int version)
    {
        return db.BullseyeRevisions.AsNoTracking()
            .Where(e => e.ApplicationId == appId && e.Version == version)
            .ToListAsync();
    }
    public Task<List<BullseyeRevisionModel>> GetAllForAppWithRevision(Guid appId, int version)
    {
        return db.BullseyeRevisions.AsNoTracking()
            .Where(e => e.ApplicationId == appId && e.Version == version)
            .ToListAsync();
    }

    public Task<bool> ExistsByApplicationAndVersion(Guid appId, int version)
    {
        return db.BullseyeRevisions
            .AnyAsync(e => e.ApplicationId == appId && e.Version == version);
    }
    public Task<bool> ExistsByApplicationAndTag(Guid appId, string? tag)
    {
        tag = tag?.Trim().ToLower().ReplaceLineEndings("");
        return db.BullseyeRevisions.AsNoTracking()
            .AnyAsync(e => e.ApplicationId == appId && e.Tag == tag);
    }

    public Task<List<BullseyeRevisionModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public Task<List<BullseyeRevisionModel>> GetAllUsingFile(Guid fileId)
    {
        return db.BullseyeRevisions.AsNoTracking().Where(e
            => e.ArchiveStorageFileId == fileId
            || e.PeerToPeerStorageFileId == fileId
            || e.SignatureStorageFileId == fileId)
            .ToListAsync();
    }
    public Task<bool> ExistsById(Guid id)
    {
        return db.BullseyeRevisions.AnyAsync(e => e.Id == id);
    }

    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.BullseyeRevisions
                .Where(e => ids.Contains(e.Id))
                .ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return count;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task<BullseyeRevisionModel> InsertOrUpdate(BullseyeRevisionModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.BullseyeRevisions.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.BullseyeRevisions.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.PreviousRevisionId, model.PreviousRevisionId)
                    .SetProperty(p => p.Version, model.Version)
                    .SetProperty(p => p.Tag, model.Tag)
                    .SetProperty(p => p.ArchiveStorageFileId, model.ArchiveStorageFileId)
                    .SetProperty(p => p.Size, model.Size)
                    .SetProperty(p => p.PeerToPeerStorageFileId, model.PeerToPeerStorageFileId)
                    .SetProperty(p => p.SignatureStorageFileId, model.SignatureStorageFileId)
                    .SetProperty(p => p.IsLive, model.IsLive)
                    .SetProperty(p => p.CreatedAt, model.CreatedAt)
                    .SetProperty(p => p.PublishAt, model.PublishAt)
                    .SetProperty(p => p.CreatedByUserId, model.CreatedByUserId));
            }
            else
            {
                await ctx.BullseyeRevisions.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return await ctx.BullseyeRevisions.SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
