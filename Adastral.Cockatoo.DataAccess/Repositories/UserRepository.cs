using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Adastral.Cockatoo.DataAccess.Repositories;

public class UserRepository(ApplicationDbContext db)
{
    public Task<UserModel?> GetById(Guid id)
    {
        return db.Users.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }
}
