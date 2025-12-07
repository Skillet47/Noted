using BusinessLogic.Core;

namespace BusinessLogic.Notes;

/// <summary>
/// Represents a simple note for capturing ideas.
/// This is the most basic note type with no additional properties beyond the base <see cref="Note"/> class.
/// </summary>
/// <remarks>
/// Idea notes are useful for quick thoughts, brainstorming, or any content
/// that doesn't require reminders or task tracking.
/// </remarks>
public class IdeaNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.Idea;
}
