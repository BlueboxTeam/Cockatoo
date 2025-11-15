using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class BlogPostAttachmentRepository(ApplicationDbContext db)
{
    public Task<List<BlogPostAttachmentModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public Task<List<BlogPostAttachmentModel>> GetAllUsingFile(Guid storageFileId)
    {
        return db.BlogPostAttachments.AsNoTracking()
            .Where(e => e.StorageFileId == storageFileId).ToListAsync();
    }
    public Task<List<BlogPostAttachmentModel>> GetAllForBlogPost(Guid blogPostId)
    {
        return db.BlogPostAttachments.AsNoTracking()
            .Where(e => e.BlogPostId == blogPostId).ToListAsync();
    }

    public Task<List<BlogPostAttachmentModel>> DeleteForPost(params BlogPostModel[] posts)
        => DeleteForPost(posts.Select(v => v.Id).ToArray());
    public async Task<List<BlogPostAttachmentModel>> DeleteForPost(params Guid[] blogPostIds)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            var models = await ctx.BlogPostAttachments.AsNoTracking().Where(e => blogPostIds.Contains(e.BlogPostId)).ToListAsync();
            await ctx.BlogPostAttachments.Where(e => blogPostIds.Contains(e.BlogPostId)).ExecuteDeleteAsync();


            await ctx.SaveChangesAsync();
            await trans.CommitAsync();
            return models;
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<long> Delete(params IEnumerable<BlogPostAttachmentModel> models)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            long count = 0;
            foreach (var m in models)
            {
                count += await ctx.BlogPostAttachments
                    .Where(e => e.BlogPostId == m.BlogPostId && e.StorageFileId == m.StorageFileId)
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

    public async Task InsertOrUpdate(BlogPostAttachmentModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (!await ctx.BlogPostAttachments.AnyAsync(e => e.BlogPostId == model.BlogPostId && e.StorageFileId == model.StorageFileId))
            {
                await ctx.BlogPostAttachments.AddAsync(model);
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
