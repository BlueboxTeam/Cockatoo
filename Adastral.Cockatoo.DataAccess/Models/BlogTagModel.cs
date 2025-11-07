using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogTagModel
{
    public const string TableName = "BlogTag";

    public Guid Id { get; set; }

    /// <summary>
    /// Name of the Tag
    /// </summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = "";
}