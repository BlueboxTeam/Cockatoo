using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserModel : IdentityUser<Guid>
{
    public const string TableName = "AspNetUsers";
    
    [MaxLength(100)]
    public string? ThemeName { get; set; }

    [MaxLength(100)]
    public string? SteamUserId { get; set; }

    [DefaultValue(false)]
    public bool IsServiceAccount { get; set; } = false;
}
public enum CanUserCreateTokenKind
{
    /// <summary>
    /// The provided requesting user can create a token for the target user.
    /// </summary>
    Yes,
    /// <summary>
    /// Returned when the Target User provided is not a service account.
    /// </summary>
    TargetUserIsNotServiceAccount,
    /// <summary>
    /// Requesting user is not the owner of the target service account, and it doesn't have <see cref="PermissionKind.ServiceAccountAdmin"/>
    /// </summary>
    RequestingUserIsNotOwner,
    /// <summary>
    /// The requesting user is a Service Account, and thus cannot create tokens for any other service account.
    /// </summary>
    RequestingUserIsServiceAccount,
}