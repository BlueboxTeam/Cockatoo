
namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostAuthorModel
{
    public const string TableName = "BlogPostAuthor";

    public Guid BlogPostId { get; set; }
    public Guid UserId { get; set; }

    // Property Accessor
    public UserModel User { get; set; } = null;
}