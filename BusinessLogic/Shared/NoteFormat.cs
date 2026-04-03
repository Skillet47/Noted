using BusinessLogic.Core.Features.Notes;

namespace BusinessLogic.Shared;

/// <summary>
/// Defines the content format types available for notes.
/// Each format determines how the note content is stored and rendered.
/// </summary>
/// <remarks>
/// When adding a new format:
/// 1. Add the new enum value here
/// 2. Update <see cref="NoteManagement"/> to handle the file extension mapping
/// 3. Update the UI components to render the format appropriately
/// </remarks>
public enum NoteFormat
{
    /// <summary>
    /// Plain text format. Stored as .txt files.
    /// Content is displayed as-is without any formatting.
    /// </summary>
    PlainText,

    /// <summary>
    /// Markdown format. Stored as .md files.
    /// Content supports Markdown syntax for formatting (headers, lists, links, etc.).
    /// </summary>
    Markdown,

    /// <summary>
    /// Rich text format. Stored as .rtf files.
    /// Content supports rich formatting including fonts, colors, and styles.
    /// </summary>
    RichText
}
