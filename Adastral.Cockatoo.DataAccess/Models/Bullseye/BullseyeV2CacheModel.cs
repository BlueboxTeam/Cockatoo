using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BullseyeV2CacheModel
{
    public const string TableName = "BullseyeCacheV2";

    public BullseyeV2CacheModel()
    {
        ApplicationId = Guid.Empty;
        IsLive = false;
        Content = new();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Is this current cached model live/published?
    /// </summary>
    [DefaultValue(false)]
    public bool IsLive { get; set; }

    public BullseyeV2 Content { get; set; }

    /// <summary>
    /// When this Cache Model was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}