using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BullseyeV2CacheRepository(ApplicationDbContext db)
{
    public async Task<BullseyeV2CacheModel?> GetById(Guid id)
    {
        throw new NotImplementedException();
    }
    public async Task<BullseyeV2CacheModel?> GetByAppId(Guid appId, bool? includeLive = null)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyeV2CacheModel>> GetAllForAppId(Guid appId, bool? includeLive = null)
    {
        throw new NotImplementedException();
    }
    public async Task<List<BullseyeV2CacheModel>> GetAll(bool? includeLive = null)
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

    public async Task<BullseyeV2CacheModel> InsertOrUpdate(BullseyeV2CacheModel model)
    {
        throw new NotImplementedException();
    }
}
