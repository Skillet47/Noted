using BusinessLogic.Core.Features.Notes;

namespace BusinessLogic.Shared;

/// <summary>
/// Defines the different categories of notes available in the application.
/// Each type has specific behavior and properties associated with it.
/// </summary>
/// <remarks>
/// When adding a new note type:
/// 1. Add the new enum value here
/// 2. Create a corresponding class in the Notes namespace that inherits from <see cref="Note"/>
/// 3. Update <see cref="NoteManagement"/> to handle serialization/deserialization
/// 4. Update the UI components to display type-specific fields
/// </remarks>
public enum NoteType
{
    /// <summary>
    /// A general-purpose note with no type-specific metadata.
    /// </summary>
    General,

    /// <summary>
    /// A note with an associated reminder date/time that triggers notifications.
    /// </summary>
    Reminder,

    /// <summary>
    /// A note representing a task with a completion status (NotStarted, InProgress, Completed).
    /// </summary>
    Task,

    /// <summary>
    /// A note for ideation workflows with idea-specific stage tracking.
    /// </summary>
    Idea
}
