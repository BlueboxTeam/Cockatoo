
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationSourceModModel
{
    public const string TableName = "ApplicationSourceMod";

    /// <summary>
    /// Primary Key and Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Source Mod Directory Name for this application
    /// </summary>
    [JsonPropertyName("sm_name")]
    [MaxLength(64)]
    public string FolderName { get; set; } = "";
    
    /// <summary>
    /// Shortened name that is used as a prefix for an applications archives/patches, like <c>of</c> for Open Fortress, since it's patch filenames are like <c>of19-21.pwr</c>
    /// </summary>
    [JsonPropertyName("short_name")]
    [MaxLength(32)]
    public string ShortName { get; set; } = "";

    /// <summary>
    /// Stylized version of the Application Name to use in a Launcher or Updater.
    /// </summary>
    [JsonPropertyName("name_stylized")]
    [MaxLength(255)]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Base Steam App ID that this Application depends on to run.
    /// </summary>
    [JsonPropertyName("base_appid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? BaseAppId { get; set; } = 0;

    /// <summary>
    /// An array of required Steam App Ids that are required before launching this application.
    /// </summary>
    [JsonPropertyName("required_appids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    // TODO figure out if this can be stored as a json in Postgres
    public uint[]? RequiredAppIds { get; set; }

    /// <summary>
    /// Is Proton required for this application?
    /// </summary>
    [JsonPropertyName("require_proton")]
    [DefaultValue(false)]
    public bool RequireProton { get; set; }
}