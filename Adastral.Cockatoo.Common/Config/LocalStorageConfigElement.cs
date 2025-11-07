using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

[Category("Storage - Local")]
public class LocalStorageConfigElement
{
    /// <summary>
    /// Use a Local Directory for file storage
    /// </summary>
    [DefaultValue(true)]
    [Description("Use a Local Directory for file storage")]
    [XmlAttribute("Enable")]
    public bool Enable { get; set; } = true;

    /// <summary>
    /// Location to where storage should be when <see cref="Enable"/> is <see langword="true"/>
    /// </summary>
    [DefaultValue("./storage")]
    [Description("Location to where storage should be when `Enable` is `true`")]
    [XmlAttribute("Location")]
    public string Location { get; set; } = "./storage";
}