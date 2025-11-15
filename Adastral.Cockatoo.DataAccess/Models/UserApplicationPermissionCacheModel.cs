namespace Adastral.Cockatoo.DataAccess.Models;

public class UserApplicationPermissionCacheModel
{
    public const string TableName = "UserApplicationPermissionCache";

    public UserApplicationPermissionCacheModel()
    {
        UserId = Guid.Empty;
        ApplicationId = Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public ScopedApplicationPermissionKind Permission { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
