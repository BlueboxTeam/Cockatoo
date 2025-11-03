using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationBullseyeModel
{
    public const string TableName = "ApplicationBullseye";

    /// <summary>
    /// Primary Key and Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Latest <see cref="BullseyeRevisionModel"/> for this App.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeRevisionModel.Id"/>
    /// </remarks>
    [DefaultValue(null)]
    public Guid? LatestRevisionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public BullseyeV1CacheModel? CacheV1 { get; set; }
    public BullseyeV2CacheModel? CacheV2 { get; set; }
}