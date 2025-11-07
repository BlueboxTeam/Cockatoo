namespace Adastral.Cockatoo.DataAccess.Models;

public class BasePermissionGroupModel<TKind>
    where TKind : struct, Enum
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GroupModel.Id"/>
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Permission Kind
    /// </summary>
    public TKind Kind { get; set; }
    
    /// <summary>
    /// Should the permission kind be allowed?
    /// When <see langword="false"/> it will not allow it.
    /// </summary>
    public bool Allow { get; set; }

    #region Property Accessor
    public GroupModel Group { get; set; } = null;
    #endregion
}