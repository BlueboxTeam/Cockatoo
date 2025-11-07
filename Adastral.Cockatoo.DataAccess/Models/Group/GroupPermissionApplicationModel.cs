using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupPermissionApplicationModel
    : BasePermissionGroupModel<ScopedApplicationPermissionKind>
{
    public const string TableName = "CockatooGroupPermissionApplication";
    
    /// <summary>
    /// When <see langword="null"/>, this applies to app Applications that are owned by the <see cref="GroupModel"/>
    /// associated with this document.
    /// </summary>
    public Guid? ApplicationId { get; set; }

    #region Property Accessors
    public ApplicationModel? Application { get; set; }
    #endregion
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