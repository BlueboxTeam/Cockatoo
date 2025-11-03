namespace Adastral.Cockatoo.DataAccess.Models;

public class PermissionRoleModel
{
    public const string TableName = "CockatooGroupPermissionRole";

    public Guid Id { get; set; }

    public PermissionKind Kind { get; set; } = PermissionKind.Login;

    /// <summary>
    /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
    /// </summary>
    public bool Allow { get; set; }

    /// <summary>
    /// Foreign Key <see cref="PermissionGroupModel.Id"/>
    /// </summary>
    public Guid PermissionGroupId { get; set; }
}
