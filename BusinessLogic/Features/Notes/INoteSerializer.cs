using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;

namespace BusinessLogic.Features.Notes;

/// <summary>
/// Public interface for serializing and deserializing notes to and from the file-based storage format.
/// Allows external services to work with note files.
/// </summary>
public interface INoteSerializer
{
    /// <summary>
    /// Reads a note from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the note file</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The deserialized note, or null if the file is not a valid note file</returns>
    Task<Note?> ReadNoteFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the file content for a note in the Noted format.
    /// </summary>
    /// <param name="note">The note to serialize</param>
    /// <returns>The serialized note content as a string</returns>
    string BuildNoteFileContent(Note note);

    /// <summary>
    /// Gets the file extension for a given note format.
    /// </summary>
    /// <param name="format">The note format</param>
    /// <returns>The file extension (e.g., ".txt", ".md", ".rtf")</returns>
    string GetFileExtension(NoteFormat format);

    /// <summary>
    /// Gets the note format from a file extension.
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".txt", ".md")</param>
    /// <returns>The note format, or PlainText if not recognized</returns>
    NoteFormat GetFormatFromExtension(string extension);

    IReadOnlyDictionary<string, NoteFormat> SupportedExtensions { get; }
    string OriginalFolderMetadataExtension { get; }
    string ContentDelimiter { get; }
}
