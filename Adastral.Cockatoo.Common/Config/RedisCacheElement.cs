using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class RedisCacheElement
{
    [DefaultValue(true)]
    [XmlAttribute("Enable")]
    public bool Enable { get; set; }

    [DefaultValue(true)]
    [XmlElement(nameof(EnableLogging))]
    public bool EnableLogging { get; set; }

    [Required]
    [XmlElement("DBConfig")]
    public RedisCacheDatabaseOptionsElement DbConfig { get; set; } = new();

    public class RedisCacheEndpointElement
    {
        [Required]
        [XmlAttribute(nameof(Host))]
        public string Host { get; set; }

        [Required]
        [XmlAttribute(nameof(Port))]
        public int Port { get; set; }

        public RedisCacheEndpointElement()
            : this("127.0.0.1", 6379)
        { }

        public RedisCacheEndpointElement(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }

    public class RedisCacheDatabaseOptionsElement
    {
        #region RedisDBOptions
        [DefaultValue(0)]
        [XmlElement(nameof(Database))]
        public int Database { get; set; } = 0;

        [DefaultValue(10000)]
        [XmlElement(nameof(SyncTimeout))]
        public int SyncTimeout { get; set; } = 10000;

        [DefaultValue(10000)]
        [XmlElement(nameof(AsyncTimeout))]
        public int AsyncTimeout { get; set; } = 10000;
        #endregion

        #region BaseRedisOptions
        [DefaultValue(null)]
        [XmlElement(nameof(Username))]
        public string? Username { get; set; }

        [DefaultValue(null)]
        [XmlElement(nameof(Password))]
        public string? Password { get; set; }

        [DefaultValue(false)]
        [XmlElement(nameof(SslEnabled))]
        public bool SslEnabled { get; set; } = false;

        [DefaultValue(null)]
        [XmlElement(nameof(SslHost))]
        public string? SslHost { get; set; }

        /// <summary>
        /// Default value for <see cref="ConnectionTimeout"/>. <c>10000</c>
        /// </summary>
        public const int ConnectionTimeoutDefault = 10000;

        [DefaultValue(ConnectionTimeoutDefault)]
        [XmlElement(nameof(ConnectionTimeout))]
        public int ConnectionTimeout { get; set; } = ConnectionTimeoutDefault;

        [DefaultValue(false)]
        [XmlElement(nameof(AllowAdmin))]
        public bool AllowAdmin { get; set; } = false;

        [DefaultValue(true)]
        [XmlElement(nameof(AbortOnConnectFail))]
        public bool AbortOnConnectFail { get; set; } = true;

        [XmlElement("Endpoint")]
        public List<RedisCacheEndpointElement> Endpoints { get; set; } = [];
        #endregion
    }
}