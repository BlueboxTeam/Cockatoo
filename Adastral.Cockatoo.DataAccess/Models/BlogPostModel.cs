using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostModel
{
    public const string TableName = "BlogPost";

    public BlogPostModel()
        : base()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Blog Post Title. Markdown only.
    /// </summary>
    [MaxLength(100)]
    public string Title { get; set; } = "";
    /// <summary>
    /// Content is mixed Markdown and HTML.
    /// </summary>
    public string Content { get; set; } = "";
    /// <summary>
    /// Is this blog post live? When not, it is only visible for users with <see cref="PermissionKind.ApplicationViewAllBlogPosts"/>
    /// </summary>
    [DefaultValue(false)]
    public bool IsLive { get; set; } = false;
    /// <summary>
    /// When not null, this can be used as a slug for this blog post. Slugs are unique, and insert/update should be prevented if the slug exists already (when not null)
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Timestamp when this Blog Post was created at (Unix Epoch, UTC, Seconds)
    /// </summary>
    [Description("Timestamp when this blog post was created.")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// <see cref="ApplicationDetailModel.Id"/> that is associated with this blog post.
    /// </summary>
    [Description("Application Id that is associated with this blog post (if there is any)")]
    [DefaultValue(null)]
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// <see cref="BullseyeAppRevisionModel.Id"/> that is associated with this blog post (if there is any).
    /// </summary>
    [Description("Bullseye Revision that is associated with this blog post (if there is any)")]
    [DefaultValue(null)]
    public Guid? BullseyeRevisionId { get; set; }


    #region IsDeleted
    [DefaultValue(false)]
    public bool IsDeleted { get; set; } = false;
    [DefaultValue(null)]
    public DateTimeOffset? DeletedAt { get; set; }
    [DefaultValue(null)]
    public Guid? DeletedByUserId { get; set; }
    #endregion

    #region Property Accessors
    /// <summary>
    /// List of <see cref="UserModel.Id"/> for who created this blog post.
    /// </summary>
    [Description("List of Users that will be displayed as authors.")]
    public List<BlogPostAuthorModel> Authors { get; set; } = [];
    #endregion
}