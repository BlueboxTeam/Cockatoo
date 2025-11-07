using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

[Category("Storage")]
public class StorageConfigElement
{
    [XmlElement("Local")]
    public LocalStorageConfigElement Local { get; set; } = new();

    [XmlElement("FileApi")]
    public FileApiConfigElement FileApi { get; set; } = new();

    [XmlElement("S3")]
    public S3ConfigElement S3 { get; set; } = new();
    
    [XmlAttribute("DoProxy")]
    [Description("When enabled, all files will be proxied through this Cockatoo instance.")]
    public bool Proxy { get; set; }
}