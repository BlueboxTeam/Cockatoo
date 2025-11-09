using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

[XmlRoot("AppConfig")]
public class AppConfig
{
    private static readonly Lock _internalGetLock = new();
    private static AppConfig InternalGet()
    {
        lock (_internalGetLock)
        {
            if (InternalInstance != null) return InternalInstance;
            
            var location = Path.GetFullPath(FeatureFlags.ConfigLocation);
            if (!File.Exists(location))
            {
                throw new InvalidOperationException($"Cannot get config since {location} doesn't exist (via {nameof(FeatureFlags)}.{nameof(FeatureFlags.ConfigLocation)})");
            }
            var i = new AppConfig();
            i.ReadFromFile(location);
            return i;
        }
    }
    private static AppConfig? InternalInstance { get; set; }

    public static AppConfig Instance
    {
        get
        {
            if (InternalInstance == null)
            {
                InternalInstance = InternalGet();
            }
            return InternalInstance;
        }
    }

    private readonly Lock _filesystemLock = new();
    public void WriteToFile(string location)
    {
        lock (_filesystemLock)
        {
            using var file = new FileStream(location, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            file.SetLength(0);
            file.Seek(0, SeekOrigin.Begin);
            Write(file);
        }
    }

    public void ReadFromFile(string location)
    {
        lock (_filesystemLock)
        {
            if (!File.Exists(location))
            {
                throw new ArgumentException($"{location} does not exist", nameof(location));
            }

            var content = File.ReadAllText(location);
            var xmlSerializer = new XmlSerializer(GetType());
            var xmlTextReader = new XmlTextReader(new StringReader(content)) {XmlResolver = null};
            var data = (AppConfig?)xmlSerializer.Deserialize(xmlTextReader);
            if (data == null)
            {
                return;
            }

            foreach (var p in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                p.SetValue(this, p.GetValue(data));
            }

            foreach (var f in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                f.SetValue(this, f.GetValue(data));
            }
        }
    }

    public void Write(Stream stream)
    {
        var serializer = new XmlSerializer(GetType());
        var options = new XmlWriterSettings()
        {
            Indent = true
        };
        using var writer = XmlWriter.Create(stream, options);
        serializer.Serialize(writer, this);
    }

    [XmlElement("Auth")]
    public AuthConfigElement Auth { get; set; } = new();

    [XmlElement("Storage")]
    public StorageConfigElement Storage { get; set; } = new();

    [XmlElement("Database")]
    public PostgreSqlConfigElement Database { get; set; } = new();

    [XmlElement("Cache")]
    public CacheConfigElement Cache { get; set; } = new();
    
    [XmlElement("Kestrel")]
    public KestrelConfigElement? Kestrel { get; set; }
    
    [XmlElement("Smtp")]
    public SmtpConfigElement? Smtp { get; set; }
    
    [XmlElement("Proxy")]
    public ProxyConfigElement? Proxy { get; set; }

    [XmlElement("Sentry")]
    public SentryConfigElement? Sentry { get; set; }

    [XmlElement("PublicUrl")]
    [Description("Base Url for public access. Must not have a trailing slash (like `https://cockatoo.example.com`)")]
    [DefaultValue("http://localhost:6280")]
    public string PublicUrl { get; set; } = "http://localhost:6280";

    [XmlElement("PartnerUrl")]
    [Description("Url that should be used for Partners when publishing applications. (optional)")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartnerUrl { get; set; }
}