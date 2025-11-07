using Adastral.Cockatoo.Common;
using Npgsql;

namespace Adastral.Cockatoo.DataAccess;

public static class DatabaseHelper
{
    
    public static string ToConnectionString(this PostgreSqlConfigElement element)
    {
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = element.Host,
            Port = element.Port,
            Username = element.Username,
            Password = element.Password,
            Database = element.Name,
            ApplicationName = "Adastral.Cockatoo"
        };
        return b.ToString();
    }
}