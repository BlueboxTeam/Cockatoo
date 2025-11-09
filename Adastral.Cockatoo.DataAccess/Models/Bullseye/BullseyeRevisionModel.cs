using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BullseyeRevisionModel
{
    public const string TableName = "BullseyeRevision";

    public BullseyeRevisionModel()
    {
        Id = Guid.NewGuid();
        ApplicationId = Guid.Empty;
        Size = 0;
        IsLive = false;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="ApplicationBullseyeModel.ApplicationId"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Previous revision that was released before this one.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeRevisionModel.Id"/>
    /// </remarks>
    [DefaultValue(null)]
    public Guid? PreviousRevisionId { get; set; }

    /// <summary>
    /// <para><b>Version number</b></para>
    /// Unique within <see cref="ApplicationId"/>
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Tag for this revision. Used for displaying in-app. This is unique db-wide
    /// </summary>
    [MaxLength(100)]
    public string? Tag { get; set; } // TODO fluent config for unique index

    /// <summary>
    /// Id for the Storage File that contains the full Archive of this revision.
    /// </summary>
    /// <remarks>
    /// Foreign Key Constraint to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    [DefaultValue(null)]
    public Guid? ArchiveStorageFileId { get; set; }


    /// <summary>
    /// Decompressed size of the file at in <see cref="ArchiveStorageFileId"/>
    /// </summary>
    public long Size { get; set; }

    [DefaultValue(null)]
    public Guid? PeerToPeerStorageFileId { get; set; }

    /// <summary>
    /// Id for the Storage File that contains the signature file.
    /// </summary>
    /// <remarks>
    /// Foreign Key Constraint to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    [DefaultValue(null)]
    public Guid? SignatureStorageFileId { get; set; }

    /// <summary>
    /// Is this current revision live? When it is, it should be available in <see cref="BullseyeV1"/>/<see cref="BullseyeV2"/>.
    /// </summary>
    [DefaultValue(false)]
    public bool IsLive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When set, this revision should be scheduled at the time provided to set the <see cref="IsLive"/> property to <see langword="true"/>
    /// </summary>
    [DefaultValue(null)]
    public DateTimeOffset? PublishAt { get; set; }

    [DefaultValue(null)]
    public Guid? CreatedByUserId { get; set; }
}