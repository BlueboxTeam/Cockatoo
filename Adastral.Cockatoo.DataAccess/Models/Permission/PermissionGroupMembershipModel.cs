
namespace Adastral.Cockatoo.DataAccess.Models;

public class PermissionGroupMembershipModel
{
    public const string TableName = "CockatooPermissionGroupMembership";

    public Guid UserId { get; set; }
    public Guid PermissionGroupId { get; set; }
}