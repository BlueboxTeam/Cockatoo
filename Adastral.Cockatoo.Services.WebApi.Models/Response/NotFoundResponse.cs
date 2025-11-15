using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class NotFoundResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;

    [Required]
    [JsonRequired]
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [Required]
    [JsonRequired]
    [JsonPropertyName("prop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PropertyName { get; set; }

    [JsonPropertyName("propType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PropertyParentType { get; set; }
}

public class NotFoundException : Exception
{
    public string? PropertyName { get; private set; }
    public string? PropertyParentType { get; private set; }

    public NotFoundException(NotFoundResponse data)
        : base(data.Message)
    {
        PropertyName = data.PropertyName;
        PropertyParentType = data.PropertyParentType;
    }

    public override string ToString()
    {
        var s = base.ToString();
        if (!string.IsNullOrEmpty(PropertyName))
        {
            var data = new string[2]
            {
                $"{nameof(PropertyName)}: {PropertyName}", ""
            };
            if (!string.IsNullOrEmpty(PropertyParentType))
            {
                data[2] = $"{nameof(PropertyParentType)}: {PropertyParentType}";
            }

            s += "\n" + string.Join("\n", data.Where(v => v.Length > 0));
        }

        return s;
    }
}