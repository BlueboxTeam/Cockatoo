
namespace Adastral.Cockatoo.DataAccess.Models;

public class ServiceAccountModel
{
    public const string TableName = "ServiceAccount";
    
    public Guid UserId { get; set; }
    public Guid OwnerUserId { get; set; }
}