using System.ComponentModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

// TODO fix me please
public class AdminUserV1DetailResponse /*: IUserModel*/
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type { get; private set; } = nameof(AdminUserV1DetailResponse);

    #region IUserModel
    [JsonRequired]
    public Guid Id { get; set; }
    /// <inheritdoc/>
    [DefaultValue(null)]
    public string? Username { get; set; }
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SteamUserId { get; set; }
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OAuthUserId { get; set; }
    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool IsServiceAccount { get; set; }
    #endregion
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? AvatarFileId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? OwnerUserId { get; set; }

    public void FromModel(UserModel user)
    {
        Id = user.Id;
        Username = user.UserName;
        Email = user.Email;
        SteamUserId = user.SteamUserId;
        // OAuthUserId = user.OAuthUserId;
        IsServiceAccount = user.IsServiceAccount;
    }

    // public void FromModel(UserPreferencesModel? preferences)
    // {
    //     AvatarFileId = preferences?.AvatarStorageFileId?.ToLower();
    // }

    public void FromModel(ServiceAccountModel? serviceAccount)
    {
        OwnerUserId = serviceAccount?.OwnerUserId;
    }
}