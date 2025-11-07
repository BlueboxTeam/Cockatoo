namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// <see cref="Microsoft.AspNetCore.Identity.IdentityRole{TKey}"/> scoped to an <see cref="ApplicationModel"/>
/// </summary>
public class ScopedApplicationRoleModel
{
    public const string TableName = "AspNetApplicationRoles";

    public ScopedApplicationRoleModel()
    {
        Id = Guid.NewGuid();
        ApplicationId = Guid.Empty;
    }
    public ScopedApplicationRoleModel(string roleName)
        : this()
    {
        Name = roleName;
    }

    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Returns the name of the role.
    /// </summary>
    /// <returns>The name of the application role.</returns>
    public override string ToString()
    {
        return Name ?? string.Empty;
    }

    #region Property Accessors
    public ApplicationModel Application { get; set; } = null;
    #endregion
}