namespace BusinessLogic.Core.Enums;

/// <summary>
/// Color-coded tags for visual categorization and prioritization of notes.
/// Tags are displayed as colored indicators on note cards.
/// </summary>
public enum NoteTag
{
    /// <summary>
    /// No tag assigned to the note.
    /// </summary>
    None,

    /// <summary>
    /// Red tag, typically used for high priority items.
    /// </summary>
    Red,

    /// <summary>
    /// Orange tag for medium-high priority or urgent items.
    /// </summary>
    Orange,

    /// <summary>
    /// Yellow tag for moderate priority or attention-needed items.
    /// </summary>
    Yellow,

    /// <summary>
    /// Green tag, often used for completed or low priority items.
    /// </summary>
    Green,

    /// <summary>
    /// Blue tag for informational or reference items.
    /// </summary>
    Blue,

    /// <summary>
    /// Purple tag for personal or special category items.
    /// </summary>
    Purple
}
