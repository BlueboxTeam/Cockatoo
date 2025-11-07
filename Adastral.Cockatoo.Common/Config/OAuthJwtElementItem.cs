using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class OAuthJwtElementItem
{
    [Required]
    [XmlAttribute("name")]
    public string InternalName { get; set; } = "";
    
    [Required]
    [XmlText]
    public string JwtValue { get; set; } = "";
}