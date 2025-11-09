using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;

public class AutoUpdaterDotNetRevisionModel
{
    public const string TableName = "AutoUpdaterDotNetRevision";

    public AutoUpdaterDotNetRevisionModel()
    {
        Id = Guid.NewGuid();
        ApplicationId = Guid.Empty;
        Version = "0.0.0.0";
        StorageFileId = Guid.Empty;
        IsEnabled = false;
        Mandatory = false;
        MandatoryKind = AutoUpdaterDotNetMandatoryKind.Normal;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Id for the <see cref="ApplicationModel"/> that this instance is for.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </remarks>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Version. Formatted like <c>X.X.X.X</c>
    /// </summary>
    [DefaultValue("0.0.0.0")]
    public string Version { get; set; }

    /// <summary>
    /// Id for the Storage File that contains the update.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public Guid StorageFileId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, this revision is publicly available (unless the application is private, or can only be read by a specific organization)
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// <b>Optional</b>: You can provide the path of the executable if it was changed in the update. It should be relative to the installation directory of the application.
    /// For example, if the new executable is located inside the bin folder of the installation directory, then you should provide it as shown below.
    /// <code>
    /// &lt;executable&gt;bin\AutoUpdaterTest.exe&lt;/executable&gt;
    /// </code>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// <b>Optional:</b> Launch arguments for the <see cref="ExecutablePath"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecutableLaunchArguments { get; set; }

    /// <summary>
    /// <para><b>Optional:</b> You need to provide URL of the change log of your application between changelog tags.</para>
    /// If you don't provide the URL of the changelog then update dialog won't show the change log.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChangelogUrl { get; set; }

    /// <summary>
    /// <para>You can set this to true if you don't want user to skip this version.</para>
    /// This will ignore Remind Later and Skip options and hide both Skip and Remind Later button on update dialog.
    /// </summary>
    [DefaultValue(false)]
    public bool Mandatory { get; set; }

    /// <summary>
    /// You can provide mode attribute on mandatory element to change the behavior of the mandatory flag.
    /// </summary>
    [DefaultValue(AutoUpdaterDotNetMandatoryKind.Normal)]
    public AutoUpdaterDotNetMandatoryKind MandatoryKind { get; set; }

    /// <summary>
    /// <para><b>Optional:</b> You can also provide minVersion attribute on mandatory element.</para>
    /// When you provide it, Mandatory option will be triggered only if the installed version of the app is less than the minimum version you specified here.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MandatoryMinimumVersion { get; set; }

    /// <summary>
    /// Time when this revision was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Time when this revision was updated (UTC)
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum AutoUpdaterDotNetMandatoryKind
{
    /// <summary>
    /// In this mode, it ignores Remind Later and Skip values set previously and hide both buttons.
    /// </summary>
    [XmlEnum("0")]
    Normal = 0,
    /// <summary>
    /// Hide the Close button on update dialog
    /// </summary>
    [XmlEnum("1")]
    Forced = 1,
    /// <summary>
    /// In this mode, it will start downloading and applying update without showing standard update dialog in addition to
    /// Forced mode behavior.
    /// </summary>
    [XmlEnum("2")]
    ForcedDownload = 2
}