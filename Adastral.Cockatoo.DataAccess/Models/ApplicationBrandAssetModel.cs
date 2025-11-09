using System.ComponentModel;
using System.Security.Cryptography;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationBrandAssetModel
{
    public const string TableName = "ApplicationBrandAsset";

    public ApplicationBrandAssetModel()
    {
        ApplicationId = Guid.Empty;
        IsManagedFile = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    /// <remarks>
    /// Clustered Primary Key with <see cref="Type"/>
    /// </remarks>
    public Guid ApplicationId { get; set; }
    /// <summary>
    /// Asset type
    /// </summary>
    /// <remarks>
    /// Clustered Primary Key with <see cref="ApplicationId"/>
    /// </remarks>
    public ApplicationBrandAssetType Type { get; set; }

    /// <summary>
    /// Is the file for this image managed by Cockatoo? If so, then <see cref="StorageFileRepository"/> should be used
    /// to get the Sha256 of the file.
    /// </summary>
    public bool IsManagedFile { get; set; }

    /// <summary>
    /// Sha256 Hash of the content at <see cref="Url"/>
    /// </summary>
    /// <remarks>
    /// Only set when <see cref="IsManagedFile"/> is <see langword="false"/>
    /// </remarks>
    [DefaultValue(DbGlobals.EmptySha256Hash)]
    public string? Sha256Hash
    {
        get;
        set => field = value?.Replace("-", "").ToUpper();
    }

    /// <summary>
    /// Url for the asset.
    /// </summary>
    /// <remarks>
    /// Only set when <see cref="IsManagedFile"/> is <see langword="false"/>
    /// </remarks>
    [DefaultValue(null)]
    public string? Url { get; set; }

    public async Task<bool> UpdateHash()
    {
        var client = new HttpClient();
        var res = await client.GetAsync(Url);
        if (!res.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await res.Content.ReadAsStreamAsync();
        var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        Sha256Hash = BitConverter.ToString(hash);
        return false;
    }

    /// <summary>
    /// <see cref="StorageFileModel.Id"/> when <see cref="IsManagedFile"/> is set to <see langword="true"/>
    /// </summary>
    [DefaultValue(null)]
    public Guid? StorageFileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    #region Property Accessors
    public StorageFileModel? StorageFile { get; set; }
    #endregion
}

public enum ApplicationBrandAssetType
{
    Icon,
    Star,
    Wordmark,
    Background
}
