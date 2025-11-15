namespace Adastral.Cockatoo.DataAccess.Models;

public class UserGlobalPermissionCacheModel
{
    public const string TableName = "UserGlobalPermissionCache";
    public UserGlobalPermissionCacheModel()
    {
        UserId = Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Foreign key to <see cref="UserModel"/>
    /// </summary>
    public Guid UserId { get; set; }
    public PermissionKind Permission { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
