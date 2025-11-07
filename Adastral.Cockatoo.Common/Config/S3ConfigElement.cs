using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

[Category("Storage - S3")]
public class S3ConfigElement
{
    /// <summary>
    /// Enable the usage of S3 for file storage
    /// </summary>
    [XmlElement("Enable")]
    [DefaultValue(false)]
    [Description("Enable the usage of S3 for file storage")]
    public bool Enable { get; set; }

    /// <summary>
    /// Bucket Name to use
    /// </summary>
    [XmlElement("Bucket")]
    [DefaultValue("")]
    [Description("Bucket Name to use")]
    public string BucketName { get; set; } = "";

    /// <summary>
    /// Access Key Id for S3 Client
    /// </summary>
    [XmlElement("AccessKeyId")]
    [DefaultValue("")]
    [Description("Access Key Id for S3 Client")]
    public string AccessKeyId { get; set; } = "";

    /// <summary>
    /// Access Secret Key for S3 Client
    /// </summary>
    [XmlElement("AccessSecretKey")]
    [DefaultValue("")]
    [Description("Access Secret Key for S3 Client")]
    public string AccessSecretKey { get; set; } = "";

    /// <summary>
    /// AWS S3-Compatible Service URL
    /// </summary>
    [XmlElement("ServiceUrl")]
    [DefaultValue("")]
    [Description("AWS S3-Compatible Service URL")]
    public string ServiceUrl { get; set; } = "";

    /// <summary>
    /// Is the S3 Service URL not AWS (e.g; Cloudflare R2)
    /// </summary>
    [XmlElement("NotUsingAWS")]
    [DefaultValue(false)]
    [Description("Is the S3 Service URL not AWS (e.g; Cloudflare R2)")]
    public bool NotUsingAWS { get; set; }

    /// <summary>
    /// Provide a specific region for authentication
    /// </summary>
    [XmlElement("AuthenticationRegion")]
    [DefaultValue(null)]
    [Description("Provide a specific region for authentication")]
    public string? AuthenticationRegion { get; set; }
}