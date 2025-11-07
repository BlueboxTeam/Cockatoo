using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class OAuthJwtElement
{
    [Required]
    [XmlElement("Item")]
    public List<OAuthJwtElementItem> Items { get; set; } = [];
}