using System.ComponentModel;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class PostgreSqlConfigElement
{
    [DefaultValue("postgres")]
    [XmlAttribute("Host")]
    public string Host { get; set; } = "postgres";

    [DefaultValue(5432)]
    [XmlAttribute("Port")]
    public int Port { get; set; } = 5432;

    [DefaultValue("cockatoo")]
    [XmlAttribute("Name")]
    public string Name { get; set; } = "cockatoo";

    [DefaultValue("postgres")]
    [XmlElement("Username")]
    public string Username { get; set; } = "postgres";

    [DefaultValue("")]
    [XmlElement("Password")]
    public string Password { get; set; } = "";
}