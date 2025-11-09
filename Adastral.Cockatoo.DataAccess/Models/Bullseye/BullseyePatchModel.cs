using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BullseyePatchModel
{
    public const string TableName = "BullseyePatch";

    public BullseyePatchModel()
    {
        Id = Guid.NewGuid();
        ApplicationId = Guid.Empty;
        FromRevisionId = Guid.Empty;
        ToRevisionId = Guid.Empty;
        StorageFileId = Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="ApplicationBullseyeModel.ApplicationId"/>/<see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Revision that this patch will be upgrading from
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel.Id"/>
    /// </remarks>
    public Guid FromRevisionId { get; set; }
    
    /// <summary>
    /// Revision that this patch will be upgrading to
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel.Id"/>
    /// </remarks>
    public Guid ToRevisionId { get; set; }

    /// <summary>
    /// Id for <see cref="StorageFileModel"/> that contains the Butler Patch file.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public Guid StorageFileId { get; set; }
    
    /// <summary>
    /// Id for <see cref="StorageFileModel"/> that contains the <c>.torrent</c> file that contains the Butler Patch file.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    [DefaultValue(null)]
    public Guid? PeerToPeerStorageFileId { get; set; }

    /// <summary>
    /// When this patch was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}