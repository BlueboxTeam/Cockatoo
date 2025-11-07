using System.Collections.ObjectModel;
using System.Text;

namespace Adastral.Cockatoo.Common;

public class VerificationErrorItem
{
    public Type Type { get; private set; }
    public string PropertyName { get; private set; }
    public string Message { get; private set; }
    public ReadOnlyCollection<VerificationErrorItem> InnerErrors { get; private set; }

    public VerificationErrorItem(Type type, string propertyName, string message, List<VerificationErrorItem>? innerErrors = null)
    {
        Type = type;
        PropertyName = propertyName;
        Message = message;
        InnerErrors = (innerErrors ?? []).AsReadOnly();
    }

    public string FormatAsString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("{0}.{1}: {2}", Type, PropertyName, Message);
        if (InnerErrors.Count > 0)
        {
            foreach (var error in InnerErrors)
            {
                var str = error.FormatAsString();
                sb.AppendLine("  - " + str);
            }
        }

        return sb.ToString();
    }
}