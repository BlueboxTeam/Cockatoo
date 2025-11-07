using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class CacheConfigElement
{
    [XmlElement("InMemory")]
    public InMemoryCacheElement? InMemory { get; set; }

    [XmlElement("Redis")]
    public RedisCacheElement? Redis { get; set; }

    /// <summary>
    /// Default value for <see cref="CachePrefix"/>: <c>EF_Cockatoo_</c>
    /// </summary>
    public const string CachePrefixDefault = "EF_Cockatoo_";

    [DefaultValue(CachePrefixDefault)]
    [XmlElement(nameof(CachePrefix))]
    public string CachePrefix { get; set; } = CachePrefixDefault;
}