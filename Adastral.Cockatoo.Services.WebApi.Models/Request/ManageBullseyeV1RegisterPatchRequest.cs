using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageBullseyeV1RegisterPatchRequest
{
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.FromRevisionId"/>
    /// </summary>
    [JsonPropertyName("from")]
    public Guid FromRevisionId { get; set; } = Guid.Empty;
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.ToRevisionId"/>
    /// </summary>
    [JsonPropertyName("to")]
    public Guid ToRevisionId { get; set; } = Guid.Empty;
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.StorageFileId"/>
    /// </summary>
    [JsonPropertyName("patch")]
    public Guid PatchFileId { get; set; } = Guid.Empty;
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.PeerToPeerStorageFileId"/> (optional)
    /// </summary>
    [JsonPropertyName("p2p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? PeerToPeerFileId { get; set; }
}