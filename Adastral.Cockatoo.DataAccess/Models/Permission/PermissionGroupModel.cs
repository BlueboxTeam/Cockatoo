using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models;
public class PermissionGroupModel
{
    public PermissionGroupModel()
        : base()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public const string TableName = "CockatooPermissionGroup";

    public Guid Id { get; set; }

    /// <summary>
    /// Permission Group name
    /// </summary>
    public string Name { get; set; } = "New Permission Group";

    /// <summary>
    /// When the permission group was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Ordering Priority. <see cref="uint.MaxValue"/> is the most important, and <see cref="uint.MinValue"/> is the least important.
    /// </summary>
    public uint Priority { get; set; }
}
