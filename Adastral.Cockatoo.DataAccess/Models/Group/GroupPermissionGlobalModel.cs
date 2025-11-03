
using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupPermissionGlobalModel
{
    public const string TableName = "CockatooGroupPermissionGlobal";
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GroupModel"/>
    /// </summary>
    public Guid GroupId { get; set; }

    public PermissionKind Kind { get; set; } = PermissionKind.Login;

    /// <summary>
    /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
    /// </summary>
    [DefaultValue(true)]
    public bool Allow { get; set; }
}