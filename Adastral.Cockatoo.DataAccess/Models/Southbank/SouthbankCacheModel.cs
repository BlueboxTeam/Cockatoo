namespace Adastral.Cockatoo.DataAccess.Models;

public class SouthbankCacheModel
{
    public const string TableName = "SouthbankCacke";

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

    public SouthbankV1 V1 { get; set; }
    public SouthbankV2 V2 { get; set; }
    public SouthbankV3 V3 { get; set; }
}