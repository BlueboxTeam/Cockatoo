using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class StorageFileModel
{
    public const string TableName = "StorageFile";
    private const string DefaultContentType = "application/octet-stream";

    public StorageFileModel()
    {
        ContentType = DefaultContentType;
        Location = "";
        CreatedAt = DateTimeOffset.UtcNow;
        Sha256Hash = "".PadRight(64, '0');
        Size = null;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Sha256 Hash of the content at the Location specified.
    /// </summary>
    [MaxLength(128)]
    public string Sha256Hash { get; set; }

    /// <summary>
    /// Location to this file in the current configured S3-compatible bucket.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// MIME type for the file (like <c>image/png</c>)
    /// </summary>
    [DefaultValue(DefaultContentType)]
    public string ContentType { get; set; }

    /// <summary>
    /// File size (bytes)
    /// </summary>
    [DefaultValue(null)]
    public long? Size { get; set; }

    /// <summary>
    /// When this file was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// When this file was updated (UTC)
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool HasHash()
    {
        return !string.IsNullOrEmpty(Sha256Hash) || Sha256Hash != "".PadRight(64, '0');
    }
}