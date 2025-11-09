using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BlogPostTagRepository(ApplicationDbContext db)
{
    public Task<List<BlogPostTagModel>> GetManyForPost(Guid postId)
    {
        return db.BlogPostTags.Include(e => e.BlogTag)
            .AsNoTracking()
            .Where(e => e.BlogPostId == postId)
            .ToListAsync();
    }
    public Task<bool> Exists(Guid postId, Guid tagId)
    {
        return db.BlogPostTags
            .AnyAsync(e => e.BlogPostId == postId && e.BlogTagId == tagId);
    }
    public async Task<long> Delete(params IEnumerable<BlogPostTagModel> postTags)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            long count = 0;
            foreach (var postTag in postTags)
            {
                count += await ctx.BlogPostTags
                    .Where(e => e.BlogTagId == postTag.BlogTagId && e.BlogPostId == postTag.BlogPostId)
                    .ExecuteDeleteAsync();
            }
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
    public async Task<long> DeleteForPost(params IEnumerable<Guid> blogPostIds)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var count = await ctx.BlogPostTags
                .Where(e => blogPostIds.Contains(e.BlogPostId))
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
    public async Task InsertOrUpdate(params IEnumerable<BlogPostTagModel> postTags)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            foreach (var postTag in postTags)
            {
                if (!await ctx.BlogPostTags.AnyAsync(e => e.BlogPostId == postTag.BlogPostId && e.BlogTagId == postTag.BlogTagId))
                {
                    await ctx.BlogPostTags.AddAsync(postTag);
                }
            }
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
}
