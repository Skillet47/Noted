using BusinessLogic.Core.Enums;
using BusinessLogic.Core.Models;

namespace BusinessLogic.Notes;

/// <summary>
/// Represents the recurrence pattern for a reminder note.
/// </summary>
public enum RecurrencePattern
{
    None,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Represents a note with an associated reminder date and time.
/// When the reminder time is reached, a notification is triggered.
/// </summary>
/// <remarks>
/// Reminder notifications are managed by the <c>INotificationService</c> in the Noted project.
/// The notification scheduling is handled when the note is saved or updated.
/// </remarks>
public class ReminderNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.Reminder;

    /// <summary>
    /// The date and time when the reminder notification should be triggered.
    /// </summary>
    public required DateTime ReminderDateTime { get; init; }

    /// <summary>
    /// The recurrence pattern for the reminder (None, Daily, Weekly, Monthly).
    /// </summary>
    public RecurrencePattern Recurrence { get; init; } = RecurrencePattern.None;
}
