namespace Adastral.Cockatoo.DataAccess.Models;

public class SouthbankCacheModel
{
    public const string TableName = "SouthbankCache";

    public SouthbankCacheModel()
    {
        ApplicationId = Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        V1 = new();
        V2 = new();
        V3 = new();
    }

    /// <summary>
    /// Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// When this record was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Cached Southbank V1 (stored as Json in database)
    /// </summary>
    public SouthbankV1 V1 { get; set; }
    /// <summary>
    /// Cached Southbank V2 (stored as Json in database)
    /// </summary>
    public SouthbankV2 V2 { get; set; }
    /// <summary>
    /// Cached Southbank V3 (stored as Json in database)
    /// </summary>
    public SouthbankV3 V3 { get; set; }
}