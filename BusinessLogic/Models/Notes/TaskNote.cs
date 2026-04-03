using BusinessLogic.Shared;

namespace BusinessLogic.Models.Notes;

/// <summary>
/// Represents a note that tracks a task with a completion status.
/// Tasks can be marked as Not Started, In Progress, or Completed.
/// </summary>
/// <remarks>
/// Task status can be updated directly from the note card in the UI
/// without opening the edit modal, providing quick status updates.
/// </remarks>
public class TaskNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.Task;

    /// <summary>
    /// The current completion status of the task.
    /// Defaults to <see cref="NoteTaskStatus.NotStarted"/>.
    /// </summary>
    public NoteTaskStatus Status { get; init; } = NoteTaskStatus.NotStarted;
}
