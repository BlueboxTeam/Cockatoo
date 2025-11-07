using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.Common;

public class SmtpConfigElement
{
    /// <summary>
    /// Domain/Address/FQDN for the SMTP server.
    /// </summary>
    [Required]
    [XmlElement(nameof(Domain))]
    public string Domain { get; set; } = "";

    /// <summary>
    /// Port for the SMTP Server.
    /// </summary>
    [DefaultValue(25)]
    [XmlElement(nameof(Port))]
    public int Port { get; set; } = 25;

    /// <summary>
    /// Use SSL for SMTP
    /// </summary>
    [DefaultValue(false)]
    [XmlElement(nameof(UseSsl))]
    public bool UseSsl { get; set; } = false;
    
    [Required]
    [XmlElement(nameof(SenderAddress))]
    public string SenderAddress { get; set; } = "";
    
    [Required]
    [XmlElement(nameof(SenderName))]
    public string SenderName { get; set; } = "";

    /// <summary>
    /// Username to use when connecting to the SMTP server.
    /// </summary>
    [XmlElement(nameof(Username))]
    public string Username { get; set; } = "";

    /// <summary>
    /// Password to use when connecting to the SMTP server
    /// </summary>
    [XmlElement(nameof(Password))]
    public string Password { get; set; } = "";

    /// <summary>
    /// Encoding to use when authenticating with the SMTP server.
    /// </summary>
    /// <remarks>
    /// Ignored when <see cref="EnableAuthentication"/> is <see langword="false"/>
    /// </remarks>
    [DefaultValue("Default")]
    [XmlElement(nameof(AuthEncoding))]
    public string AuthEncoding { get; set; } = "Default";

    /// <summary>
    /// Valid values for <see cref="AuthEncoding"/>
    /// </summary>
    public static readonly ICollection<string> AuthEncodingValues
        = [
        "Default",
        "UTF8",
        "UTF32",
        "Unicode",
        "BigEndianUnicode",
        "Latin1",
        "ASCII"
    ];

    /// <summary>
    /// Get the Encoding from <see cref="AuthEncoding"/>
    /// </summary>
    public Encoding GetAuthEncoding()
    {
        switch (AuthEncoding)
        {
            case "UTF8":
                return Encoding.UTF8;
            case "UTF32":
                return Encoding.UTF32;
            case "Unicode":
                return Encoding.Unicode;
            case "BigEndianUnicode":
                return Encoding.BigEndianUnicode;
            case "Latin1":
                return Encoding.Latin1;
            case "ASCII":
                return Encoding.ASCII;
            default:
                return Encoding.Default;
        }
    }

    [DefaultValue(false)]
    [XmlElement(nameof(EnableAuthentication))]
    public bool EnableAuthentication { get; set; }

    /// <summary>
    /// Regular Expression used to verify domains/FQDNs.
    /// https://stackoverflow.com/a/57129482/13037015
    /// </summary>
    public const string DomainRegularExpression = "^(?!.*?_.*?)(?!(?:[\\w]+?\\.)?\\-[\\w\\.\\-]*?)(?![\\w]+?\\-\\.(?:[\\w\\.\\-]+?))(?=[\\w])(?=[\\w\\.\\-]*?\\.+[\\w\\.\\-]*?)(?![\\w\\.\\-]{254})(?!(?:\\.?[\\w\\-\\.]*?[\\w\\-]{64,}\\.)+?)[\\w\\.\\-]+?(?<![\\w\\-\\.]*?\\.[\\d]+?)(?<=[\\w\\-]{2,})(?<![\\w\\-]{25})$";
    /// <summary>
    /// Regular Expression used to verify hostnames.
    /// </summary>
    /// <remarks>
    /// Conditions
    /// <list type="bullet">
    /// <item>Cannot start or end with a hyphen (RFC-952 and RFC-1123/2.1)</item>
    /// <item>Maximum length of 63 (RFC-1035/3.1 and RFC-2181/11)</item>
    /// <item>Only contain letters, numbers, and hyphens (RFC-3696/2 and RFC-2181/11)</item>
    /// </list>
    /// </remarks>
    public const string HostnameRegularExpression = "^[a-zA-Z0-9]{1}(|([a-zA-Z0-9\\-]{1,61}[a-zA-Z0-9]))$";
    public IEnumerable<VerificationErrorItem> Verify()
    {
        var result = new List<VerificationErrorItem>();
        if (string.IsNullOrEmpty(Domain))
        {
            result.Add(new(GetType(),
                nameof(Domain),
                "Cannot be null or empty"));
        }

        var hostnameRegex = new Regex(HostnameRegularExpression);
        var domainRegex = new Regex(DomainRegularExpression, RegexOptions.Multiline);
        var numericRegex = new Regex("^[0-9]{1,}$");

        // Assume that the "Domain" is a FQDN (or hostname) and not an IP address
        // when we can't parse it into System.Net.IPAddress
        if (!IPAddress.TryParse(Domain.Trim(), out var _) && !string.IsNullOrEmpty(Domain))
        {
            Domain = Domain.Trim(' ');
            var domainDecimalSplit = Domain.Split('.');
            if (domainDecimalSplit.Length == 1)
            {
                if (Domain.TrimEnd('.').Length > 253)
                {
                    result.Add(new(GetType(),
                        nameof(Domain),
                        $"Cannot be larger than 253 characters (RFC-2181)"));
                }
                else if (hostnameRegex.IsMatch(Domain) == false)
                {
                    result.Add(new(GetType(),
                        nameof(Domain),
                        $"Invalid hostname"));
                }
                else if (numericRegex.IsMatch(Domain) == false)
                {
                    result.Add(new(GetType(),
                        nameof(Domain),
                        $"Cannot only contain numbers (RFC-3696/2)"));
                }
            }
            else if (domainDecimalSplit.Length >= 2)
            {
                if (domainRegex.IsMatch(Domain) == false)
                {
                    result.Add(new(GetType(),
                        nameof(Domain),
                        $"Invalid FQDN (doesn't abide by; RFC-1035, RFC-2181, RFC-3696, RFC-952, RFC-1123)"));
                }
            }
        }
        if (EnableAuthentication)
        {
            if (string.IsNullOrEmpty(Username))
            {
                    
                result.Add(new(GetType(),
                    nameof(Username),
                    $"Required when authentication is enabled, and Windows authentication isn't being used"));
            }
            if (AuthEncodingValues.Contains(AuthEncoding) == false)
            {
                result.Add(new(GetType(),
                    nameof(AuthEncoding),
                    $"Invalid value (valid: {string.Join(", ", AuthEncodingValues)})"));
            }
        }

        return result.AsReadOnly();
    }
}