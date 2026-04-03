using BusinessLogic.Shared;

namespace BusinessLogic.Models.Notes;

/// <summary>
/// Represents a general-purpose note with no specialized metadata.
/// </summary>
public class GeneralNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.General;
}
