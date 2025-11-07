using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

[Category("Storage - File Api")]
public class FileApiConfigElement
{
    /// <summary>
    /// HTTP Endpoint for all file serving. Must be the URL for a <c>Adastral.Cockatoo.FileWebAPI</c> instance
    /// </summary>
    [DefaultValue("")]
    [Description("HTTP Endpoint for all file serving. Must be the URL for a deployed instance of `Adastral.Cockatoo.FileWebAPI`")]
    [XmlElement("Endpoint")]
    public string Endpoint { get; set; } = "";

    /// <summary>
    /// When <see cref="Endpoint"/> is provided, and this is <see langword="true"/>, then just append <see cref="Adastral.Cockatoo.DataAccess.Models.StorageFileModel.Location"/> to <see cref="StorageFileEndpoint"/>
    /// </summary>
    [DefaultValue(false)]
    [Description("When `Endpoint` is set, and this is `true`, then just append the `Location` property in `StorageFileModel` to the value of `StorageFileEndpoint`")]
    [XmlAttribute("UseDirect")]
    public bool UseDirect { get; set; }
}