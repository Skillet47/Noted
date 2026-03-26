namespace BusinessLogic.Core;

/// <summary>
/// Abstract base class representing a note in the application.
/// All specific note types (Reminder, Task, Idea) inherit from this class.
/// </summary>
/// <remarks>
/// To create a new note type:
/// 1. Create a new class that inherits from <see cref="Note"/>
/// 2. Override the <see cref="Type"/> property to return the appropriate <see cref="NoteType"/>
/// 3. Add any type-specific properties (e.g., ReminderDateTime for reminders)
/// 4. Update <see cref="NoteManagement"/> to handle the new type in RetrieveNotes and SaveNote methods
/// </remarks>
public abstract class Note
{
    /// <summary>
    /// The title/heading of the note. Used as a unique identifier for file operations.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The main body content of the note.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Timestamp when the note was originally created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp of the most recent modification to the note.
    /// </summary>
    public required DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Indicates if the note should appear at the top of the notes list.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Color-coded tag for visual organization and prioritization.
    /// </summary>
    public NoteTag Tag { get; init; } = NoteTag.None;

    /// <summary>
    /// The content format of the note (PlainText, Markdown, or RichText).
    /// Determines how the content is stored and rendered.
    /// </summary>
    public NoteFormat Format { get; init; } = NoteFormat.PlainText;

    /// <summary>
    /// Stores the original folder path when a note is moved to trash.
    /// Used to restore the note to its original location.
    /// </summary>
    public string? OriginalFolder { get; set; }

    /// <summary>
    /// The type of note. Must be implemented by derived classes.
    /// </summary>
    public abstract NoteType Type { get; }
}
