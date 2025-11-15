using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class BlogPostV1Response
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    [JsonPropertyName("body")]
    public string Body { get; set; } = "";
    [JsonPropertyName("authors")]
    public List<UserV1StrippedResponse> Authors { get; set; } = [];
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAtTimestamp { get; set; }
    [JsonPropertyName("tags")]
    public List<BlogPostV1TagResponse> Tags { get; set; } = [];

    public void FromModel(BlogPostModel model)
    {
        Id = model.Id;
        Title = model.Title;
        Body = model.Content;
        CreatedAtTimestamp = model.CreatedAt;
    }
}