using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class TaskMutexRepository(ApplicationDbContext db)
{
    /// <summary>
    /// Try and get an instance of <see cref="TaskMutexModel"/> where the <paramref name="type"/> and <paramref name="taskName"/> matches, and the lock isn't released.
    /// </summary>
    /// <remarks>
    /// When <paramref name="options"/> isn't <see langword="null"/> and all the keys in it are in a model and all of those values match, then it will return that.
    /// </remarks>
    public async Task<TaskMutexModel?> GetLockModel(Type type, string taskName, Dictionary<string, object>? options = null)
    {
        var typeName = type.ToString();
        IQueryable<TaskMutexModel> query = db.TaskMutexes.AsNoTracking()
                .Where(e => e.TaskClassType == typeName && e.TaskName == taskName && !e.Released);
        if (options == null)
        {
            return await query.FirstOrDefaultAsync();
        }
        var list = await query.ToListAsync();
        return list.FirstOrDefault(v =>
        {
            int r = options!.Keys.Count;
            int c = 0;
            foreach (var (key, value) in options)
            {
                if (v.Options?.TryGetValue(key, out var x) ?? false)
                {
                    if (x.Equals(value))
                    {
                        c++;
                    }
                }
            }
            return c == r;
        });
    }

    public async Task<TaskMutexModel> InsertOrUpdate(TaskMutexModel model)
    {
        await using var ctx = db.CreateSession();
        await using var trans = await ctx.Database.BeginTransactionAsync();
        try
        {
            if (await ctx.TaskMutexes.AnyAsync(e => e.Id == model.Id))
            {
                var now = DateTimeOffset.UtcNow;
                await ctx.TaskMutexes.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(p => p.TaskClassType, model.TaskClassType)
                        .SetProperty(p => p.TaskName, model.TaskName)
                        .SetProperty(p => p.Options, model.Options)
                        .SetProperty(p => p.Released, model.Released)
                        .SetProperty(p => p.CreatedAt, model.CreatedAt)
                        .SetProperty(p => p.UpdatedAt, now));
            }
            else
            {
                await ctx.TaskMutexes.AddAsync(model);
            }

            await ctx.SaveChangesAsync();
            await trans.CommitAsync();

            return await ctx.TaskMutexes.AsNoTracking().SingleAsync(e => e.Id == model.Id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public Task<TaskMutexModel?> GetById(Guid id)
    {
        return db.TaskMutexes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }
}
