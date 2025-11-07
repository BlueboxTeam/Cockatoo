
using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupMembershipModel
{
    public const string TableName = "CockatooGroupMembership";

    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="GroupModel"/>
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Association should be ignored when this is set to <see langword="false"/>
    /// </summary>
    [DefaultValue(false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the user was made a member of the group.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Who made this user apart of this group?
    /// </summary>
    public Guid? CreatedByUserId { get; set; }


    #region Property Accessors
    public GroupModel Group { get; set; } = null;
    public UserModel User { get; set; } = null;
    public UserModel? CreatedByUser { get; set; }
    #endregion
}