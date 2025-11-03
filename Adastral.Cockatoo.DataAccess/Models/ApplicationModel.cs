using System.ComponentModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationModel
{
    public const string TableName = "Application";

    public ApplicationModel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        IsPrivate = true;
        IsHidden = true;
        IsManaged = false;
        IsDeleted = false;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Only applies when <see cref="Type"/> is set to <see cref="ApplicationType.InternalApp"/>
    /// </summary>
    [DefaultValue(null)]
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Not Required when <see cref="Type"/> is set to <see cref="ApplicationType.InternalApp"/>
    /// </summary>
    [DefaultValue(null)]
    public string? DisplayName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApplicationType Type { get; set; }

    /// <summary>
    /// When set to <see langword="true"/>, the requesting user must have authoritative access to get anything about it.
    /// This application will also be hidden when fetching all the available ones.
    /// </summary>
    [DefaultValue(true)]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// When set to <see langword="true"/>, it will not be included in Southbank generation or when trying to view available applications when not logged in.
    /// </summary>
    [DefaultValue(true)]
    public bool IsHidden { get; set; }

    /// <summary>
    /// Is this application managed by Cockatoo? (e.g; all files are handled/managed by Cockatoo)
    /// </summary>
    [DefaultValue(false)]
    public bool IsManaged { get; set; }

    /// <summary>
    /// Is this application deleted?
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, <see cref="DeletedAt"/> should be set.
    /// </remarks>
    [DefaultValue(false)]
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid? DeletedByUserId { get; set; }

    /// <summary>
    /// Can this application be managed by Cockatoo?
    /// </summary>
    public static bool CanBeManaged(ApplicationType type)
    {
        return type == ApplicationType.Kachemak;
    }

    #region Property Accessors
    public ApplicationSourceModModel SourceMod { get; set; } = new();
    public ApplicationBrandingModel Branding { get; set; } = new();
    public SouthbankCacheModel? SouthbankCache { get; set; }
    #endregion
}

public enum ApplicationType
{
    /// <summary>
    /// Use this when you are only using Cockatoo to internally keep track of an applications version.
    /// </summary>
    [Description("Use this when you are only using Cockatoo to internally keep track of an applications version.")]
    [EnumDisplayName("Internal Application")]
    InternalApp,
    /// <summary>
    /// Kachemak/Adastral/Cockatoo Versioning System.
    /// </summary>
    [Description("Kachemak/Adastral/Cockatoo Versioning System.")]
    Kachemak,
    /// <summary>
    /// <see cref="AppVarRemoteDetail.VersionsUrl"/> must be the Github Release Listing API Url, like <c>https://api.github.com/repositories/805393469/releases</c>
    /// </summary>
    [Description("Remote Versions Url must be the Github Release Listing API Url, like `https://api.github.com/repositories/805393469/releases`.")]
    [EnumDisplayName("GitHub Releases")]
    GithubReleases,
    /// <summary>
    /// Uses the versioning system from <see href="https://github.com/ravibpatel/AutoUpdater.NET">AutoUpdater.NET</see>
    /// </summary>
    [Description("Uses the versioning system from [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)")]
    [EnumDisplayName("AutoUpdater.NET")]
    AutoUpdaterDotNet
}