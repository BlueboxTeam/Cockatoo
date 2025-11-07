using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class InMemoryCacheElement
{
    [Required]
    [XmlAttribute(nameof(Enable))]
    public bool Enable { get; set; }

    [Required]
    [XmlAttribute(nameof(Name))]
    public string Name { get; set; } = "InMemory";

    [DefaultValue(120)]
    [XmlElement(nameof(MaxRandomSeconds))]
    public int MaxRandomSeconds { get; set; }

    [DefaultValue(true)]
    [XmlElement(nameof(EnableLogging))]
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Default value for <see cref="LockMilliseconds"/>. <c>5000</c>
    /// </summary>
    public const int LockMillisecondsDefault = 5000;

    [DefaultValue(LockMillisecondsDefault)]
    [XmlElement(nameof(LockMilliseconds))]
    public int LockMilliseconds { get; set; } = LockMillisecondsDefault;

    /// <summary>
    /// Default value for <see cref="SleepMilliseconds"/>. <c>300</c>
    /// </summary>
    public const int SleepMillisecondsDefault = 300;
    [DefaultValue(SleepMillisecondsDefault)]
    [XmlElement(nameof(SleepMilliseconds))]
    public int SleepMilliseconds { get; set; } = SleepMillisecondsDefault;

    [XmlElement("DBConfig")]
    public InMemoryCacheDatabaseOptionsElement DbConfig { get; set; } = new();
    public class InMemoryCacheDatabaseOptionsElement
    {
        [DefaultValue(60)]
        [XmlElement(nameof(ExpirationScanFrequency))]
        public int ExpirationScanFrequency { get; set; }

        [DefaultValue(10000)]
        [XmlElement(nameof(SizeLimit))]
        public int SizeLimit { get; set; }

        [DefaultValue(true)]
        [XmlAttribute(nameof(EnableReadDeepClone))]
        public bool EnableReadDeepClone { get; set; }

        [DefaultValue(false)]
        [XmlAttribute(nameof(EnableWriteDeepClone))]
        public bool EnableWriteDeepClone { get; set; }
    }
}