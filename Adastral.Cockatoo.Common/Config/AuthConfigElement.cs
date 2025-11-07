using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class AuthConfigElement
{
    [XmlElement("OAuth")]
    public List<OAuthConfigElement> OAuth { get; set; } = [];

    // TODO add support for LDAP
}