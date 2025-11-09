using Adastral.Cockatoo.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BullseyeV1CacheRepository(ApplicationDbContext db)
{
    public async Task<BullseyeV1CacheModel?> GetById(Guid id)
    {
        throw new NotImplementedException();
    }
    public async Task<BullseyeV1CacheModel?> GetByAppId(Guid appId, bool? includeLive = null)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyeV1CacheModel>> GetAllForAppId(Guid appId, bool? includeLive = null)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyeV1CacheModel>> GetAll(bool? includeLive = null)
    {
        throw new NotImplementedException();
    }
    public async Task<long> DeleteForAppId(params IEnumerable<Guid> appIds)
    {
        throw new NotImplementedException();
    }
    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        throw new NotImplementedException();
    }

    public async Task<BullseyeV1CacheModel> InsertOrUpdate(BullseyeV1CacheModel model)
    {
        throw new NotImplementedException();
    }
}
