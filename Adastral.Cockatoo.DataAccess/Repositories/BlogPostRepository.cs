using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BlogPostRepository
{
    private readonly ApplicationDbContext _db;
    public BlogPostRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<BlogPostModel?> GetById(Guid id)
    {
        return _db.BlogPosts.AsNoTracking()
            .Include(e => e.Authors)
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }
    public Task<List<BlogPostModel>> GetAll(bool? liveState = null)
    {
        IQueryable<BlogPostModel> q = _db.BlogPosts.AsNoTracking().Include(e => e.Authors);
        if (liveState.HasValue)
        {
            q = q.Where(e => e.IsLive == liveState.Value);
        }
        return q.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }
    public Task<List<BlogPostModel>> GetManyForRevision(Guid revisionId, bool onlyLive)
    {
        return _db.BlogPosts
            .AsNoTracking().Include(e => e.Authors)
            .Where(e => e.BullseyeRevisionId == revisionId && (!onlyLive || e.IsLive))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
    public Task<bool> Exists(Guid id)
    {
        return _db.BlogPosts.AnyAsync(e => e.Id == id);
    }
    public async Task<bool> SlugExists(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return true;
        slug = slug.Trim().ToLower().ReplaceLineEndings("");
        return await _db.BlogPosts.AnyAsync(e => e.Slug == slug);
    }

    public Task<int> Delete(Guid? deletedByUserId, params IEnumerable<BlogPostModel> posts)
        => Delete(deletedByUserId, posts.Select(e => e.Id));
    public async Task<int> Delete(Guid? deletedByUserId, params IEnumerable<Guid> blogPostIds)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var rowcount = await ctx.BlogPosts
                .Where(e => blogPostIds.Contains(e.Id) && !e.IsDeleted)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedByUserId, deletedByUserId)
                    .SetProperty(p => p.DeletedAt, now));
            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return rowcount;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task InsertOrUpdate(BlogPostModel model)
    {
        await using var ctx = _db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            model.Slug = model.Slug?.Trim().ToLower().ReplaceLineEndings("");
            for (int i = 0; i < model.Authors.Count; i++)
            {
                model.Authors[i].BlogPostId = model.Id;
            }

            if (await ctx.BlogPosts.AnyAsync(e => e.Id == model.Id))
            {
                await ctx.BlogPosts.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Title, model.Title)
                    .SetProperty(p => p.Content, model.Content)
                    .SetProperty(p => p.IsLive, model.IsLive)
                    .SetProperty(p => p.Slug, model.Slug)
                    .SetProperty(p => p.ApplicationId, model.ApplicationId)
                    .SetProperty(p => p.BullseyeRevisionId, model.BullseyeRevisionId));
                await ctx.BlogPostAuthors.Where(e => e.BlogPostId == model.Id).ExecuteDeleteAsync();
                await ctx.BlogPostAuthors.AddRangeAsync(model.Authors);
            }
            else
            {
                await ctx.BlogPosts.AddAsync(model);
                await ctx.BlogPostAuthors.AddRangeAsync(model.Authors);
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
