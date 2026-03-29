namespace BusinessLogic.Core.Enums;

/// <summary>
/// Represents the completion status of a task note.
/// Used by <see cref="Notes.TaskNote"/> to track progress.
/// </summary>
public enum NoteTaskStatus
{
    /// <summary>
    /// Task has not been started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Task is currently being worked on.
    /// </summary>
    InProgress,

    /// <summary>
    /// Task has been completed.
    /// </summary>
    Completed
}
