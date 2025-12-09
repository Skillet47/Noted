using BusinessLogic.Notes;
using System.Text;

namespace BusinessLogic.Core;

/// <summary>
/// Manages CRUD operations for notes stored as text files on the file system.
/// Each note is stored as a separate file with a format-specific extension (.txt, .md, .rtf).
/// </summary>
/// <remarks>
/// <para>
/// <b>File Format:</b> Each note file contains the following structure:
/// <list type="number">
///     <item>Line 0: Title</item>
///     <item>Line 1: CreatedAt (ISO 8601 format)</item>
///     <item>Line 2: ModifiedAt (ISO 8601 format)</item>
///     <item>Line 3: NoteType (enum value)</item>
///     <item>Line 4: ReminderDateTime (for Reminder notes, empty otherwise)</item>
///     <item>Line 5: IsPinned (boolean)</item>
///     <item>Line 6: TaskStatus (for Task notes, empty otherwise)</item>
///     <item>Line 7: Tag (NoteTag enum value)</item>
///     <item>Line 8: Format (NoteFormat enum value)</item>
///     <item>Line 9: Content delimiter marker ("---CONTENT---")</item>
///     <item>Line 10+: Content (can span multiple lines)</item>
/// </list>
/// </para>
/// <para>
/// <b>File Naming:</b> Files are named as "{SanitizedTitle}_{CreatedAtTimestamp}.{extension}"
/// where extension is based on the NoteFormat (.txt for PlainText, .md for Markdown, .rtf for RichText)
/// </para>
/// <para>
/// <b>Adding New Note Properties:</b>
/// When adding new properties to notes, add them before the content delimiter marker,
/// </para>
/// </remarks>
/// <param name="folderPath">The directory path where note files will be stored.</param>
public class NoteManagement(string folderPath)
{
    private readonly string _folderPath = folderPath;
    
    /// <summary>
    /// Delimiter used to separate metadata from multi-line content.
    /// </summary>
    private const string ContentDelimiter = "---CONTENT---";

    /// <summary>
    /// Supported file extensions for notes, mapped to their corresponding formats.
    /// </summary>
    private static readonly Dictionary<string, NoteFormat> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", NoteFormat.PlainText },
        { ".md", NoteFormat.Markdown },
        { ".rtf", NoteFormat.RichText }
    };

    /// <summary>
    /// Gets the root folder path for notes storage.
    /// </summary>
    public string RootFolderPath => _folderPath;

    /// <summary>
    /// Gets the file extension for a given note format.
    /// </summary>
    /// <param name="format">The note format.</param>
    /// <returns>The file extension including the leading dot.</returns>
    public static string GetFileExtension(NoteFormat format) => format switch
    {
        NoteFormat.PlainText => ".txt",
        NoteFormat.Markdown => ".md",
        NoteFormat.RichText => ".rtf",
        _ => ".txt"
    };

    /// <summary>
    /// Gets the note format from a file extension.
    /// </summary>
    /// <param name="extension">The file extension including the leading dot.</param>
    /// <returns>The corresponding note format, defaulting to PlainText for unknown extensions.</returns>
    public static NoteFormat GetFormatFromExtension(string extension)
    {
        return SupportedExtensions.TryGetValue(extension, out var format) ? format : NoteFormat.PlainText;
    }

    /// <summary>
    /// Gets all subfolders within the notes storage folder.
    /// </summary>
    /// <returns>An enumerable of folder names (not full paths) found in the storage folder.</returns>
    public IEnumerable<string> GetSubfolders()
    {
        if (!Directory.Exists(_folderPath))
        {
            yield break;
        }

        foreach (var directory in Directory.EnumerateDirectories(_folderPath))
        {
            yield return Path.GetFileName(directory);
        }
    }

    /// <summary>
    /// Creates a new subfolder within the notes storage folder.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <returns>True if the folder was created successfully; false if it already exists or creation failed.</returns>
    public bool CreateFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return false;
        }

        var sanitizedName = SanitizeFileName(folderName);
        var newFolderPath = Path.Combine(_folderPath, sanitizedName);

        if (Directory.Exists(newFolderPath))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(newFolderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves all notes from the storage folder.
    /// </summary>
    /// <returns>An enumerable of all notes found in the storage folder.</returns>
    /// <remarks>
    /// Uses yield return for lazy evaluation, allowing efficient memory usage
    /// when dealing with large numbers of notes.
    /// </remarks>
    public IEnumerable<Note> RetrieveNotes()
    {
        return RetrieveNotesFromPath(_folderPath);
    }

    /// <summary>
    /// Retrieves all notes from a specific subfolder.
    /// </summary>
    /// <param name="subfolderName">The name of the subfolder to retrieve notes from, or null/empty for the root folder.</param>
    /// <returns>An enumerable of all notes found in the specified folder.</returns>
    public IEnumerable<Note> RetrieveNotes(string? subfolderName)
    {
        if (string.IsNullOrWhiteSpace(subfolderName))
        {
            return RetrieveNotesFromPath(_folderPath);
        }

        var targetPath = Path.Combine(_folderPath, subfolderName);
        return RetrieveNotesFromPath(targetPath);
    }

    /// <summary>
    /// Retrieves all notes from a specific path.
    /// </summary>
    private IEnumerable<Note> RetrieveNotesFromPath(string path)
    {
        // If the folder doesn't exist, there are no notes to retrieve
        if (!Directory.Exists(path))
        {
            yield break;
        }

        // Iterate through all supported file types in the folder
        foreach (var extension in SupportedExtensions.Keys)
        {
            var searchPattern = $"*{extension}";
            foreach (var filePath in Directory.EnumerateFiles(path, searchPattern))
            {
                var note = ReadNoteFromFile(filePath);
                if (note is not null)
                {
                    yield return note;
                }
            }
        }
    }

    /// <summary>
    /// Reads a note from a file path.
    /// </summary>
    /// <param name="filePath">The full path to the note file.</param>
    /// <returns>The parsed note, or null if the file is invalid.</returns>
    private static Note? ReadNoteFromFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        // Get format from file extension (used as fallback if not stored in metadata)
        var fileExtension = Path.GetExtension(filePath);
        var formatFromExtension = GetFormatFromExtension(fileExtension);

        // Find the content delimiter
        var delimiterIndex = Array.IndexOf(lines, ContentDelimiter);
        
        if (delimiterIndex < 0)
        {
            // Invalid format - no content delimiter found
            return null;
        }

        // Validate minimum required lines for a valid note file (at least title + 3 metadata + delimiter)
        if (delimiterIndex < 4)
        {
            return null;
        }

        // Parse common note properties
        var title = lines[0];
        var createdAt = DateTime.Parse(lines[1]);
        var modifiedAt = DateTime.Parse(lines[2]);
        var noteType = Enum.Parse<NoteType>(lines[3]);

        // Parse optional properties with fallback defaults
        var reminderDateTime = delimiterIndex >= 5 && !string.IsNullOrWhiteSpace(lines[4])
            ? DateTime.Parse(lines[4])
            : DateTime.MinValue;
        var isPinned = delimiterIndex >= 6 && bool.TryParse(lines[5], out var pinned) && pinned;
        var taskStatus = delimiterIndex >= 7 && Enum.TryParse<NoteTaskStatus>(lines[6], out var status)
            ? status
            : NoteTaskStatus.NotStarted;
        var tag = delimiterIndex >= 8 && Enum.TryParse<NoteTag>(lines[7], out var parsedTag)
            ? parsedTag
            : NoteTag.None;
        var format = delimiterIndex >= 9 && Enum.TryParse<NoteFormat>(lines[8], out var parsedFormat)
            ? parsedFormat
            : formatFromExtension;

        // Extract multi-line content (everything after the delimiter)
        var content = delimiterIndex + 1 < lines.Length
            ? string.Join(Environment.NewLine, lines.Skip(delimiterIndex + 1))
            : string.Empty;

        // Create the appropriate note type based on the stored NoteType value
        return noteType switch
        {
            NoteType.Reminder => new ReminderNote
            {
                Title = title,
                Content = content,
                CreatedAt = createdAt,
                ModifiedAt = modifiedAt,
                IsPinned = isPinned,
                Tag = tag,
                Format = format,
                ReminderDateTime = reminderDateTime
            },
            NoteType.Task => new TaskNote
            {
                Title = title,
                Content = content,
                CreatedAt = createdAt,
                ModifiedAt = modifiedAt,
                IsPinned = isPinned,
                Tag = tag,
                Format = format,
                Status = taskStatus
            },
            NoteType.Idea => new IdeaNote
            {
                Title = title,
                Content = content,
                CreatedAt = createdAt,
                ModifiedAt = modifiedAt,
                IsPinned = isPinned,
                Tag = tag,
                Format = format
            },
            _ => throw new InvalidOperationException($"Unknown note type: {noteType}")
        };
    }

    /// <summary>
    /// Saves a new note to the storage folder.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <remarks>
    /// Creates the storage folder if it doesn't exist.
    /// The filename is generated from the sanitized title and creation timestamp.
    /// </remarks>
    public void SaveNote(Note note)
    {
        if (string.IsNullOrWhiteSpace(note?.Title))
            return;

        SaveNote(note, null);
    }

    /// <summary>
    /// Saves a new note to a specific subfolder.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <param name="subfolderName">The name of the subfolder to save the note to, or null/empty for the root folder.</param>
    public void SaveNote(Note note, string? subfolderName)
    {
        if (string.IsNullOrWhiteSpace(note?.Title))
            return;

        var targetPath = string.IsNullOrWhiteSpace(subfolderName)
            ? _folderPath
            : Path.Combine(_folderPath, subfolderName);

        // Ensure the storage folder exists
        Directory.CreateDirectory(targetPath);

        // Generate a unique filename using title, timestamp, and format-appropriate extension
        var extension = GetFileExtension(note.Format);
        var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(targetPath, fileName);

        // Build the file content with all note properties
        var content = BuildNoteFileContent(note);
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Deletes a note by its title.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <returns>True if the note was found and deleted; otherwise, false.</returns>
    public bool DeleteNote(string noteTitle)
    {
        return DeleteNote(noteTitle, null);
    }

    /// <summary>
    /// Deletes a note by its title from a specific subfolder.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <param name="subfolderName">The name of the subfolder to delete from, or null/empty for the root folder.</param>
    /// <returns>True if the note was found and deleted; otherwise, false.</returns>
    public bool DeleteNote(string noteTitle, string? subfolderName)
    {
        var targetPath = string.IsNullOrWhiteSpace(subfolderName)
            ? _folderPath
            : Path.Combine(_folderPath, subfolderName);

        if (!Directory.Exists(targetPath))
        {
            return false;
        }

        // Search through all supported file types to find the one with the matching title
        foreach (var extension in SupportedExtensions.Keys)
        {
            var searchPattern = $"*{extension}";
            foreach (var filePath in Directory.EnumerateFiles(targetPath, searchPattern))
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length > 0 && lines[0] == noteTitle)
                {
                    File.Delete(filePath);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Updates an existing note with new data.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update (used to find the file).</param>
    /// <param name="updatedNote">The updated note data to save.</param>
    /// <returns>True if the note was found and updated; otherwise, false.</returns>
    /// <remarks>
    /// The ModifiedAt timestamp is automatically set to the current time.
    /// Note: If the format changes, the file extension is updated accordingly.
    /// </remarks>
    public bool UpdateNote(string originalTitle, Note updatedNote)
    {
        return UpdateNote(originalTitle, updatedNote, null);
    }

    /// <summary>
    /// Updates an existing note with new data in a specific subfolder.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update (used to find the file).</param>
    /// <param name="updatedNote">The updated note data to save.</param>
    /// <param name="subfolderName">The name of the subfolder to update in, or null/empty for the root folder.</param>
    /// <returns>True if the note was found and updated; otherwise, false.</returns>
    public bool UpdateNote(string originalTitle, Note updatedNote, string? subfolderName)
    {
        var targetPath = string.IsNullOrWhiteSpace(subfolderName)
            ? _folderPath
            : Path.Combine(_folderPath, subfolderName);

        if (!Directory.Exists(targetPath))
        {
            return false;
        }

        // Search through all supported file types for the file with the matching original title
        foreach (var extension in SupportedExtensions.Keys)
        {
            var searchPattern = $"*{extension}";
            foreach (var filePath in Directory.EnumerateFiles(targetPath, searchPattern))
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length > 0 && lines[0] == originalTitle)
                {
                    // Update the modification timestamp
                    updatedNote.ModifiedAt = DateTime.Now;

                    // Check if format changed - need to rename file with new extension
                    var currentExtension = Path.GetExtension(filePath);
                    var newExtension = GetFileExtension(updatedNote.Format);
                    
                    if (!currentExtension.Equals(newExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        // Format changed - delete old file and create new one with correct extension
                        File.Delete(filePath);
                        var newFileName = Path.GetFileNameWithoutExtension(filePath) + newExtension;
                        var newFilePath = Path.Combine(targetPath, newFileName);
                        var content = BuildNoteFileContent(updatedNote);
                        File.WriteAllText(newFilePath, content);
                    }
                    else
                    {
                        // Same format - update in place
                        var content = BuildNoteFileContent(updatedNote);
                        File.WriteAllText(filePath, content);
                    }
                    
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the file content string for a note.
    /// </summary>
    /// <param name="note">The note to serialize.</param>
    /// <returns>The formatted content string to write to a file.</returns>
    private static string BuildNoteFileContent(Note note)
    {
        var content = new StringBuilder();
        // Metadata first (single-line fields)
        content.AppendLine(note.Title);
        content.AppendLine(note.CreatedAt.ToString("O")); // ISO 8601 format for reliable parsing
        content.AppendLine(note.ModifiedAt.ToString("O"));
        content.AppendLine(note.Type.ToString());
        content.AppendLine(note is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);
        content.AppendLine(note.IsPinned.ToString());
        content.AppendLine(note is TaskNote task ? task.Status.ToString() : string.Empty);
        content.AppendLine(note.Tag.ToString());
        content.AppendLine(note.Format.ToString());
        // Content delimiter followed by multi-line content
        content.AppendLine(ContentDelimiter);
        content.Append(note.Content); // Use Append to avoid trailing newline
        return content.ToString();
    }

    /// <summary>
    /// Removes invalid characters from a filename to ensure it's valid for the file system.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename with invalid characters replaced by underscores.</returns>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(fileName);
        foreach (var c in invalidChars)
        {
            sanitized.Replace(c, '_');
        }
        return sanitized.ToString();
    }
}
