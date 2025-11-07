using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupModel
{
    public const string TableName = "CockatooGroup";

    public GroupModel()
    {
        Id = Guid.NewGuid();
        Name = "";
        CreatedAt = DateTimeOffset.UtcNow;
        Priority = uint.MaxValue;
    }

    public Guid Id { get; set; }
    
    /// <summary>
    /// Name of this Group.
    /// </summary>
    [MaxLength(200)]
    public string Name { get; set; }

    /// <summary>
    /// Format name to something that is not blank
    /// </summary>
    /// <returns><see cref="BaseGuidModel.Id"/> when <see cref="Name"/> is null or empty.</returns>
    public string FormatName()
    {
        return string.IsNullOrEmpty(Name)
            ? Id.ToString() : Name;
    }

    /// <summary>
    /// Unix Timestamp when this Group was created (UTC, Seconds)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }


    /// <summary>
    /// Ordering Priority. <see cref="uint.MaxValue"/> is the most important, and <see cref="uint.MinValue"/> is the least important.
    /// </summary>
    public uint Priority { get; set; }
}