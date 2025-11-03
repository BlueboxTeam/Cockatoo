using System.ComponentModel;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationBrandingModel
{
    public const string TableName = "ApplicationBranding";
    
    /// <summary>
    /// Primary Key and Foreign Key to <see cref="ApplicationModel.Id"/>
    /// </summary>
    public Guid ApplicationId { get; set; }

    [DefaultValue(null)]
    public string? IconUrl { get; set; }
    [DefaultValue(null)]
    public string? StarUrl { get; set; }
    [DefaultValue(null)]
    public string? WordmarkUrl { get; set; }
    [DefaultValue(null)]
    public string? BackgroundUrl { get; set; }

    [DefaultValue(null)]
    public string? ColorDark { get; set; }
    [DefaultValue(null)]
    public string? ColorLight { get; set; }
    [DefaultValue(null)]
    public string? ColorMain { get; set; }
    [DefaultValue(null)]
    public string? ColorAccent { get; set; }
    [DefaultValue(null)]
    public string? ColorSecondary { get; set; }
    [DefaultValue(null)]
    public string? ColorLightForeground { get; set; }
    [DefaultValue(null)]
    public string? ColorClick { get; set; }
    [DefaultValue(null)]
    public string? ColorClickT { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </summary>
    public Guid? IconStorageFileId { get; set; }
    [DefaultValue(null)]
    public Guid? StarStorageFileId { get; set; }
    [DefaultValue(null)]
    public Guid? WordmarkStorageFileId { get; set; }
    [DefaultValue(null)]
    public Guid? BackgroundStorageFileId { get; set; }
}