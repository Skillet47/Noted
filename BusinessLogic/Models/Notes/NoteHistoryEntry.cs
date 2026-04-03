using BusinessLogic.Shared;
using System.Text.Json.Serialization;

namespace BusinessLogic.Models.Notes;

/// <summary>
/// Represents a captured snapshot of a note before a change was applied.
/// </summary>
public record NoteHistoryEntry
{
    /// <summary>
    /// Timestamp when this history entry was captured.
    /// </summary>
    public required DateTime ChangedAtUtc { get; init; }

    /// <summary>
    /// Title of the note at the time of the snapshot.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Content of the note at the time of the snapshot.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Original creation timestamp of the note.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last modified timestamp of the note when the snapshot was taken.
    /// </summary>
    public required DateTime ModifiedAt { get; init; }

    /// <summary>
    /// Indicates whether the note was pinned when the snapshot was taken.
    /// </summary>
    public required bool IsPinned { get; init; }

    /// <summary>
    /// Tag applied to the note when the snapshot was taken.
    /// </summary>
    public required NoteTag Tag { get; init; }

    /// <summary>
    /// Content format of the note when the snapshot was taken.
    /// </summary>
    public required NoteFormat Format { get; init; }

    /// <summary>
    /// Type of note captured in this snapshot.
    /// </summary>
    public required NoteType Type { get; init; }

    /// <summary>
    /// Reminder date/time for reminder notes.
    /// </summary>
    public DateTime? ReminderDateTime { get; init; }

    /// <summary>
    /// Recurrence pattern for reminder notes.
    /// </summary>
    public RecurrencePattern? Recurrence { get; init; }

    /// <summary>
    /// Task status for task notes.
    /// </summary>
    public NoteTaskStatus? TaskStatus { get; init; }

    /// <summary>
    /// Idea stage for idea notes.
    /// </summary>
    [JsonPropertyName("IdeaStage")]
    public IdeaStage? StoredIdeaStage { get; init; }

    /// <summary>
    /// Creates a history entry snapshot from a note.
    /// </summary>
    public static NoteHistoryEntry FromNote(Note note, DateTime changedAtUtc)
    {
        return new NoteHistoryEntry
        {
            ChangedAtUtc = changedAtUtc,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            ModifiedAt = note.ModifiedAt,
            IsPinned = note.IsPinned,
            Tag = note.Tag,
            Format = note.Format,
            Type = note.Type,
            ReminderDateTime = note is ReminderNote reminder ? reminder.ReminderDateTime : null,
            Recurrence = note is ReminderNote reminderNote ? reminderNote.Recurrence : null,
            TaskStatus = note is TaskNote task ? task.Status : null,
            StoredIdeaStage = note is IdeaNote idea ? idea.Stage : null
        };
    }

    /// <summary>
    /// Recreates a note instance from this history entry.
    /// </summary>
    public Note ToNote()
    {
        return Type switch
        {
            NoteType.General => new GeneralNote
            {
                Title = Title,
                Content = Content,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                IsPinned = IsPinned,
                Tag = Tag,
                Format = Format
            },
            NoteType.Reminder => new ReminderNote
            {
                Title = Title,
                Content = Content,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                IsPinned = IsPinned,
                Tag = Tag,
                Format = Format,
                ReminderDateTime = ReminderDateTime ?? DateTime.MinValue,
                Recurrence = Recurrence ?? RecurrencePattern.None
            },
            NoteType.Task => new TaskNote
            {
                Title = Title,
                Content = Content,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                IsPinned = IsPinned,
                Tag = Tag,
                Format = Format,
                Status = TaskStatus ?? NoteTaskStatus.NotStarted
            },
            NoteType.Idea => new IdeaNote
            {
                Title = Title,
                Content = Content,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                IsPinned = IsPinned,
                Tag = Tag,
                Format = Format,
                Stage = StoredIdeaStage.GetValueOrDefault(IdeaStage.Seed)
            },
            _ => throw new InvalidOperationException($"Unknown note type: {Type}")
        };
    }
}
