using BusinessLogic.Core;

namespace BusinessLogic.Notes;

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
    public required DateTime ReminderDateTime { get; set; }
}
