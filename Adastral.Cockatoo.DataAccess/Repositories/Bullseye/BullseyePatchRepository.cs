using Adastral.Cockatoo.DataAccess.Models;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BullseyePatchRepository(ApplicationDbContext db)
{
    public async Task<BullseyePatchModel?> GetById(Guid id)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyePatchModel>> GetAllRevisionFrom(Guid fromRevisionId)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyePatchModel>> GetAllRevisionTo(Guid toRevisionId)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyePatchModel>> GetAllWithRevision(Guid revisionId)
    {
        throw new NotImplementedException();
    }
    public Task<List<BullseyePatchModel>> GetAllUsingFile(StorageFileModel file) => GetAllUsingFile(file.Id);
    public async Task<List<BullseyePatchModel>> GetAllUsingFile(Guid storageFileId)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyePatchModel>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<long> Delete(params IEnumerable<BullseyePatchModel> models) => Delete(models.Select(v => v.Id).Distinct());
    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        throw new NotImplementedException();
    }

    public async Task<BullseyePatchModel> InsertOrUpdate(BullseyePatchModel model)
    {
        throw new NotImplementedException();
    }
}
