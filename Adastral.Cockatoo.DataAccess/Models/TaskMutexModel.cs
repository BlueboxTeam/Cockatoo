using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models;

public class TaskMutexModel
{
    public const string TableName = "TaskMutex";

    public Guid Id { get; set; }

    /// <summary>
    /// Type name for the Class that the Task is running on.
    /// </summary>
    public string TaskClassType { get; set; } = "";

    /// <summary>
    /// Name of the Method or Task that this Mutex is for.
    /// </summary>
    public string TaskName { get; set; } = "";

    /// <summary>
    /// Options that were used for this Task (stored as JSON)
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }

    /// <summary>
    /// Has this mutex been released?
    /// </summary>
    public bool Released { get; set; }

    /// <summary>
    /// Timestamp when this Task Mutex was created at. (UTC, Seconds)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when this Task Mutex was last updated at. (UTC, Seconds)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
