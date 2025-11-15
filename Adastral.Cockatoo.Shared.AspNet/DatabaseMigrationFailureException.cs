using System.Collections.Immutable;

namespace Adastral.Cockatoo.Common.AspNet;

public class DatabaseMigrationFailureException : Exception
{
    public DatabaseMigrationFailureException(List<string> migrations, Exception innerException)
        : base("Failed to apply one or more database migrations", innerException)
    {
        Migrations = migrations.ToImmutableList();
    }
    /// <summary>
    /// List of migrations (in order) that were going to be applied.
    /// </summary>
    public IReadOnlyList<string> Migrations { get; set; }
}
