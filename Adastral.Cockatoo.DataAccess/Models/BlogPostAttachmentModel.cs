using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostAttachmentModel
{
    public const string TableName = "BlogPostAttachment";

    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="BlogPostModel.Id"/>
    /// </summary>
    [Required]
    public Guid BlogPostId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </summary>
    [Required]
    public Guid StorageFileId { get; set; }

    #region Property Accessors
    public BlogPostModel BlogPost { get; set; }
    public StorageFileModel StorageFile { get; set; }
    #endregion
}