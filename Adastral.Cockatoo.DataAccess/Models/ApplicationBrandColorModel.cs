using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationBrandColorModel
{
    public const string TableName = "ApplicationBrandColor";

    public ApplicationBrandColorModel()
    {
        ApplicationId = Guid.Empty;
        Value = null;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Color Type
    /// </summary>
    public ApplicationBrandColorType Type { get; set; }

    /// <summary>
    /// Hex color (like: <c>#09fe09</c>)
    /// </summary>
    [MaxLength(10)]
    public string? Value { get; set; }

    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// UTC Time when this record was updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}


public enum ApplicationBrandColorType
{
    Dark = 0,
    Light = 1,
    Main = 2,
    Accent = 3,
    Secondary = 4,
    LightForeground = 5,
    Click = 6,
    ClickT = 7
}