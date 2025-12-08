using BusinessLogic.Notes;
using System.Text;

namespace BusinessLogic.Core;

/// <summary>
/// Manages CRUD operations for notes stored as text files on the file system.
/// Each note is stored as a separate .txt file with a specific format.
/// </summary>
/// <remarks>
/// <para>
/// <b>File Format:</b> Each note file contains the following lines:
/// <list type="number">
///     <item>Line 0: Title</item>
///     <item>Line 1: Content</item>
///     <item>Line 2: CreatedAt (ISO 8601 format)</item>
///     <item>Line 3: ModifiedAt (ISO 8601 format)</item>
///     <item>Line 4: NoteType (enum value)</item>
///     <item>Line 5: ReminderDateTime (for Reminder notes, empty otherwise)</item>
///     <item>Line 6: IsPinned (boolean)</item>
///     <item>Line 7: TaskStatus (for Task notes, empty otherwise)</item>
///     <item>Line 8: Tag (NoteTag enum value)</item>
/// </list>
/// </para>
/// <para>
/// <b>File Naming:</b> Files are named as "{SanitizedTitle}_{CreatedAtTimestamp}.txt"
/// </para>
/// <para>
/// <b>Adding New Note Properties:</b>
/// When adding new properties to notes, update both the save and retrieve methods
/// to include the new data on a new line, maintaining backward compatibility with existing files.
/// </para>
/// </remarks>
/// <param name="folderPath">The directory path where note files will be stored.</param>
public class NoteManagement(string folderPath)
{
    private readonly string _folderPath = folderPath;

    /// <summary>
    /// Gets the root folder path for notes storage.
    /// </summary>
    public string RootFolderPath => _folderPath;

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

        // Iterate through all .txt files in the folder
        foreach (var filePath in Directory.EnumerateFiles(path, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            
            // Validate minimum required lines for a valid note file
            if (lines.Length >= 5)
            {
                // Parse common note properties
                var title = lines[0];
                var content = lines[1];
                var createdAt = DateTime.Parse(lines[2]);
                var modifiedAt = DateTime.Parse(lines[3]);
                var noteType = Enum.Parse<NoteType>(lines[4]);
                
                // Parse optional properties with fallback defaults for backward compatibility
                var isPinned = lines.Length >= 7 && bool.TryParse(lines[6], out var pinned) && pinned;
                var tag = lines.Length >= 9 && Enum.TryParse<NoteTag>(lines[8], out var parsedTag) ? parsedTag : NoteTag.None;

                // Create the appropriate note type based on the stored NoteType value
                yield return noteType switch
                {
                    NoteType.Reminder => new ReminderNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt,
                        IsPinned = isPinned,
                        Tag = tag,
                        ReminderDateTime = lines.Length >= 6 && !string.IsNullOrWhiteSpace(lines[5])
                            ? DateTime.Parse(lines[5])
                            : DateTime.MinValue
                    },
                    NoteType.Task => new TaskNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt,
                        IsPinned = isPinned,
                        Tag = tag,
                        Status = lines.Length >= 8 && Enum.TryParse<NoteTaskStatus>(lines[7], out var status)
                            ? status
                            : NoteTaskStatus.NotStarted
                    },
                    NoteType.Idea => new IdeaNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt,
                        IsPinned = isPinned,
                        Tag = tag
                    },
                    _ => throw new InvalidOperationException($"Unknown note type: {noteType}")
                };
            }
        }
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
        SaveNote(note, null);
    }

    /// <summary>
    /// Saves a new note to a specific subfolder.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <param name="subfolderName">The name of the subfolder to save the note to, or null/empty for the root folder.</param>
    public void SaveNote(Note note, string? subfolderName)
    {
        var targetPath = string.IsNullOrWhiteSpace(subfolderName)
            ? _folderPath
            : Path.Combine(_folderPath, subfolderName);

        // Ensure the storage folder exists
        Directory.CreateDirectory(targetPath);

        // Generate a unique filename using title and timestamp
        var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}.txt";
        var filePath = Path.Combine(targetPath, fileName);

        // Build the file content with all note properties
        var content = new StringBuilder();
        content.AppendLine(note.Title);
        content.AppendLine(note.Content);
        content.AppendLine(note.CreatedAt.ToString("O")); // ISO 8601 format for reliable parsing
        content.AppendLine(note.ModifiedAt.ToString("O"));
        content.AppendLine(note.Type.ToString());
        content.AppendLine(note is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);
        content.AppendLine(note.IsPinned.ToString());
        content.AppendLine(note is TaskNote task ? task.Status.ToString() : string.Empty);
        content.AppendLine(note.Tag.ToString());

        File.WriteAllText(filePath, content.ToString());
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

        // Search through files to find the one with the matching title
        foreach (var filePath in Directory.EnumerateFiles(targetPath, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length > 0 && lines[0] == noteTitle)
            {
                File.Delete(filePath);
                return true;
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
    /// Note: If the title changes, the file name remains the same (based on original creation).
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

        // Search for the file with the matching original title
        foreach (var filePath in Directory.EnumerateFiles(targetPath, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length > 0 && lines[0] == originalTitle)
            {
                // Update the modification timestamp
                updatedNote.ModifiedAt = DateTime.Now;

                // Rebuild the file content with updated data
                var content = new StringBuilder();
                content.AppendLine(updatedNote.Title);
                content.AppendLine(updatedNote.Content);
                content.AppendLine(updatedNote.CreatedAt.ToString("O"));
                content.AppendLine(updatedNote.ModifiedAt.ToString("O"));
                content.AppendLine(updatedNote.Type.ToString());
                content.AppendLine(updatedNote is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);
                content.AppendLine(updatedNote.IsPinned.ToString());
                content.AppendLine(updatedNote is TaskNote task ? task.Status.ToString() : string.Empty);
                content.AppendLine(updatedNote.Tag.ToString());

                File.WriteAllText(filePath, content.ToString());
                return true;
            }
        }

        return false;
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
