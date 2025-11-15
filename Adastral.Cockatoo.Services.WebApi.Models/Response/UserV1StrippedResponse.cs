using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class UserV1StrippedResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; } = 0;
    public void FromModel(UserModel model)
    {
        Id = model.Id;
        Username = model.UserName;
        // CreatedAt = model.At
    }
}