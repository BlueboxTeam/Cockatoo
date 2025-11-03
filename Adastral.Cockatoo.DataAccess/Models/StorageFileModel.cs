
namespace Adastral.Cockatoo.DataAccess.Models;

public class StorageFileModel
{
    public const string TableName = "StorageFile";

    public StorageFileModel()
    {
        ContentType = "application/octet-stream";
        Location = "";
        CreatedAtTimestamp = DateTimeOffset.UtcNow;
        UpdatedAtTimestamp = CreatedAtTimestamp;
        Sha256Hash = "".PadRight(64, '0');
        Size = null;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Sha256 Hash of the content at the Location specified.
    /// </summary>
    public string Sha256Hash { get; set; }

    /// <summary>
    /// Location to this file in the current configured S3-compatible bucket.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// MIME type for the file.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Size of the file in bytes</para>
    /// </summary>
    [DefaultValue(null)]
    public long? Size { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    
    public bool HasHash()
    {
        return !string.IsNullOrEmpty(Sha256Hash) || Sha256Hash != "".PadRight(64, '0');
    }
}