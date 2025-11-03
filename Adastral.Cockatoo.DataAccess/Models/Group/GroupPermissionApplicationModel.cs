using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupPermissionApplicationModel
{
    public const string TableName = "CockatooGroupPermissionApplication";
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GroupModel.Id"/>
    /// </summary>
    public Guid GroupId { get; set; }
    
    /// <summary>
    /// When <see langword="null"/>, this applies to app Applications that are owned by the <see cref="GroupModel"/>
    /// associated with this document.
    /// </summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// Permission
    /// </summary>
    public ScopedApplicationPermissionKind Kind { get; set; }
    
    /// <summary>
    /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
    /// </summary>
    public bool Allow { get; set; }
}

public enum ScopedApplicationPermissionKind
{
    [EnumDisplayIgnore]
    Unknown = 0x00,
    /// <summary>
    /// User can do anything related to the application
    /// </summary>
    Admin,
    EditDetails,
    EditAppearance,
    SubmitRevisions,
    ManageRevisions,
    UpdateCache,
    ReadOnly
}