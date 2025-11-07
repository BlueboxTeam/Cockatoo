using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "sb_ver")]
[JsonDerivedType(typeof(SouthbankV1), typeDiscriminator: "0.0.1")]
[JsonDerivedType(typeof(SouthbankV2), typeDiscriminator: "0.0.2")]
[JsonDerivedType(typeof(SouthbankV3), typeDiscriminator: 3)]
public class SouthbankBase
{
}