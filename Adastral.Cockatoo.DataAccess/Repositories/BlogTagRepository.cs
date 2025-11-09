using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BlogTagRepository(ApplicationDbContext db)
{
    public Task<BlogTagModel?> GetById(Guid id)
    {
        return db.BlogTags.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    public Task<List<BlogTagModel>> GetManyById(params IEnumerable<Guid> ids)
    {
        return db.BlogTags.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();
    }
    public Task<bool> Exists(Guid id)
    {
        return db.BlogTags.AnyAsync(e => e.Id == id);
    }
    public async Task<long> Delete(params IEnumerable<Guid> ids)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.BlogTags
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
    public async Task<BlogTagModel> InsertOrUpdate(BlogTagModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.BlogTags.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.BlogTags.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Name, model.Name));
            }
            else
            {
                await ctx.BlogTags.AddAsync(model);
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return model;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
