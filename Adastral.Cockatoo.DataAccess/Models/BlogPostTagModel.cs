using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostTagModel
{
    public const string TableName = "BlogPostTag";
    
    /// <summary>
    /// <see cref="BlogPostModel.Id"/>
    /// </summary>
    [Required]
    public Guid BlogPostId { get; set; }

    /// <summary>
    /// <see cref="BlogTagModel.Id"/>
    /// </summary>
    [Required]
    public Guid BlogTagId { get; set; }

    #region Property Accessors
    public BlogPostModel BlogPost { get; set; }
    public BlogTagModel BlogTag { get; set; }
    #endregion
}