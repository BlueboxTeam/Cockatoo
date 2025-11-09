using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageBullseyeV1GenerateCacheResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    /// <summary>
    /// Foreign Key to <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeV1CacheModel.Id"/>
    /// </summary>
    [JsonPropertyName("v1")]
    public Guid CacheV1Id { get; set; } = Guid.Empty;
    /// <summary>
    /// Foreign Key to <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeV2CacheModel.Id"/>
    /// </summary>
    [JsonPropertyName("v2")]
    public Guid CacheV2Id { get; set; } = Guid.Empty;
    /// <summary>
    /// Was a new record added in <see cref="Adastral.Cockatoo.DataAccess.Repositories.BullseyeAppRepository"/>
    /// </summary>
    [JsonPropertyName("isNew")]
    public bool IsNewBullseyeApp { get; set; }
}