using BusinessLogic.Core.Enums;
using BusinessLogic.Core.Models;

namespace BusinessLogic.Notes;

/// <summary>
/// Represents a general-purpose note with no specialized metadata.
/// </summary>
public class GeneralNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.General;
}
