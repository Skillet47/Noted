using BusinessLogic.Notes;
using System.Text;

namespace BusinessLogic.Core;

/// <summary>
/// Provides core note management functionality, including creating, retrieving, updating, and deleting notes and folders.
/// Supports multiple note formats and subfolder organization.
/// </summary>
public class NoteManagement(string folderPath) : INoteManagement
{
    private readonly string _folderPath = folderPath;
    private const string ContentDelimiter = "---CONTENT---";
    private const string TrashFolderName = "Trash";
    private const string OriginalFolderMetadataExtension = ".folder";
    private static readonly Dictionary<string, NoteFormat> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", NoteFormat.PlainText },
        { ".md", NoteFormat.Markdown },
        { ".rtf", NoteFormat.RichText }
    };

    public string RootFolderPath => _folderPath;
    public string TrashFolderPath => Path.Combine(_folderPath, TrashFolderName);

    public static string GetFileExtension(NoteFormat format) => format switch
    {
        NoteFormat.PlainText => ".txt",
        NoteFormat.Markdown => ".md",
        NoteFormat.RichText => ".rtf",
        _ => ".txt"
    };

    public static NoteFormat GetFormatFromExtension(string extension)
    {
        return SupportedExtensions.TryGetValue(extension, out var format) ? format : NoteFormat.PlainText;
    }

    public IEnumerable<string> GetSubfolders()
    {
        if (!Directory.Exists(_folderPath))
            yield break;

        foreach (var directory in Directory.EnumerateDirectories(_folderPath))
            yield return Path.GetFileName(directory);
    }

    public OperationResult CreateFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return OperationResult.Fail("Folder name cannot be empty.");

        var sanitizedName = SanitizeFileName(folderName);
        var newFolderPath = Path.Combine(_folderPath, sanitizedName);

        if (Directory.Exists(newFolderPath))
            return OperationResult.Fail($"Folder '{folderName}' already exists.");

        try
        {
            Directory.CreateDirectory(newFolderPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to create folder: {ex.Message}");
        }
    }

    public IEnumerable<Note> RetrieveNotes()
    {
        return RetrieveNotesFromPath(_folderPath);
    }

    public IEnumerable<Note> RetrieveNotes(string? subfolderName)
    {
        var targetPath = GetTargetPath(subfolderName);
        return RetrieveNotesFromPath(targetPath);
    }

    private IEnumerable<Note> RetrieveNotesFromPath(string path)
    {
        if (!Directory.Exists(path))
            yield break;

        foreach (var filePath in EnumerateNoteFiles(path))
        {
            var note = ReadNoteFromFile(filePath);

            if (note is not null)
                yield return note;
        }
    }

    private static Note? ReadNoteFromFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var fileExtension = Path.GetExtension(filePath);
        var formatFromExtension = GetFormatFromExtension(fileExtension);

        var delimiterIndex = Array.IndexOf(lines, ContentDelimiter);

        if (delimiterIndex < 0 || delimiterIndex < 4)
            return null;

        var title = lines[0];
        var createdAt = DateTime.Parse(lines[1]);
        var modifiedAt = DateTime.Parse(lines[2]);
        var noteType = Enum.Parse<NoteType>(lines[3]);

        var reminderDateTime = delimiterIndex >= 5 && !string.IsNullOrWhiteSpace(lines[4])
            ? DateTime.Parse(lines[4])
            : DateTime.MinValue;

        var recurrence = delimiterIndex >= 6 && Enum.TryParse<RecurrencePattern>(lines[5], out var parsedRecurrence)
            ? parsedRecurrence
            : RecurrencePattern.None;

        var isPinned = delimiterIndex >= 7 && bool.TryParse(lines[6], out var pinned) && pinned;
        var taskStatus = delimiterIndex >= 8 && Enum.TryParse<NoteTaskStatus>(lines[7], out var status)
            ? status
            : NoteTaskStatus.NotStarted;
        var tag = delimiterIndex >= 9 && Enum.TryParse<NoteTag>(lines[8], out var parsedTag)
            ? parsedTag
            : NoteTag.None;
        var format = delimiterIndex >= 10 && Enum.TryParse<NoteFormat>(lines[9], out var parsedFormat)
            ? parsedFormat
            : formatFromExtension;
        var content = delimiterIndex + 1 < lines.Length
            ? string.Join(Environment.NewLine, lines.Skip(delimiterIndex + 1))
            : string.Empty;

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
                ReminderDateTime = reminderDateTime,
                Recurrence = recurrence
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

    public OperationResult SaveNote(Note note)
    {
        return SaveNote(note, null);
    }

    public OperationResult SaveNote(Note note, string? subfolderName)
    {
        if (note is null)
            return OperationResult.Fail("Note cannot be null.");

        if (string.IsNullOrWhiteSpace(note.Title))
            return OperationResult.Fail("Note title cannot be empty.");

        try
        {
            var targetPath = GetTargetPath(subfolderName);

            Directory.CreateDirectory(targetPath);

            var extension = GetFileExtension(note.Format);
            var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(targetPath, fileName);
            var content = BuildNoteFileContent(note);

            File.WriteAllText(filePath, content);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to save note: {ex.Message}");
        }
    }

    public OperationResult DeleteNote(string noteTitle)
    {
        return DeleteNote(noteTitle, null);
    }

    public OperationResult DeleteNote(string noteTitle, string? subfolderName)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = FindNoteFileByTitle(targetPath, noteTitle);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found.");

        try
        {
            File.Delete(filePath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to delete note: {ex.Message}");
        }
    }

    public OperationResult UpdateNote(string originalTitle, Note updatedNote)
    {
        return UpdateNote(originalTitle, updatedNote, null);
    }

    public OperationResult UpdateNote(string originalTitle, Note updatedNote, string? subfolderName)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = FindNoteFileByTitle(targetPath, originalTitle);

        if (filePath is null)
            return OperationResult.Fail($"Note '{originalTitle}' not found.");

        try
        {
            updatedNote.ModifiedAt = DateTime.Now;

            var currentExtension = Path.GetExtension(filePath);
            var newExtension = GetFileExtension(updatedNote.Format);
            var content = BuildNoteFileContent(updatedNote);

            if (!currentExtension.Equals(newExtension, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(filePath);

                var newFileName = Path.GetFileNameWithoutExtension(filePath) + newExtension;
                var newFilePath = Path.Combine(targetPath, newFileName);

                File.WriteAllText(newFilePath, content);
            }
            else
            {
                File.WriteAllText(filePath, content);
            }
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to update note: {ex.Message}");
        }
    }

    public OperationResult MoveNoteToTrash(string noteTitle, string? subfolderName)
    {
        var sourcePath = GetTargetPath(subfolderName);

        if (!Directory.Exists(sourcePath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = FindNoteFileByTitle(sourcePath, noteTitle);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found.");

        try
        {
            Directory.CreateDirectory(TrashFolderPath);

            var destPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath));

            File.Move(filePath, destPath, overwrite: true);

            // Save original folder metadata
            var metadataPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath) + OriginalFolderMetadataExtension);
            File.WriteAllText(metadataPath, subfolderName ?? string.Empty);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to move note to trash: {ex.Message}");
        }
    }

    public OperationResult RestoreNoteFromTrash(string noteTitle)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = FindNoteFileByTitle(trashPath, noteTitle);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found in trash.");

        try
        {
            var metadataPath = filePath + OriginalFolderMetadataExtension;

            string? originalFolder = null;

            if (File.Exists(metadataPath))
            {
                originalFolder = File.ReadAllText(metadataPath);
            }

            var destPath = GetTargetPath(originalFolder);

            // Ensure the original folder exists (recreate if deleted)
            Directory.CreateDirectory(destPath);
            var newPath = Path.Combine(destPath, Path.GetFileName(filePath));
            File.Move(filePath, newPath, overwrite: true);

            // Delete metadata file
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to restore note from trash: {ex.Message}");
        }
    }

    public OperationResult PermanentlyDeleteNoteFromTrash(string noteTitle)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = FindNoteFileByTitle(trashPath, noteTitle);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found in trash.");

        try
        {
            File.Delete(filePath);

            var metadataPath = filePath + OriginalFolderMetadataExtension;

            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to permanently delete note: {ex.Message}");
        }
    }

    public OperationResult DeleteFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return OperationResult.Fail("Folder name cannot be empty.");

        if (folderName == TrashFolderName)
            return OperationResult.Fail("Cannot delete the Trash folder.");

        var folderPath = Path.Combine(_folderPath, folderName);
        if (!Directory.Exists(folderPath))
            return OperationResult.Fail($"Folder '{folderName}' does not exist.");

        try
        {
            // Move all notes in the folder to trash
            foreach (var noteFile in EnumerateNoteFiles(folderPath))
            {
                var note = ReadNoteFromFile(noteFile);
                if (note != null)
                {
                    MoveNoteToTrash(note.Title, folderName);
                }
            }

            // Delete the folder and its contents
            Directory.Delete(folderPath, true);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to delete folder: {ex.Message}");
        }
    }

    private static string BuildNoteFileContent(Note note)
    {
        var content = new StringBuilder();

        content.AppendLine(note.Title);
        content.AppendLine(note.CreatedAt.ToString("O"));
        content.AppendLine(note.ModifiedAt.ToString("O"));
        content.AppendLine(note.Type.ToString());

        if (note is ReminderNote reminder)
        {
            content.AppendLine(reminder.ReminderDateTime.ToString("O"));
            content.AppendLine(reminder.Recurrence.ToString());
        }
        else
        {
            content.AppendLine(string.Empty);
            content.AppendLine(string.Empty);
        }

        content.AppendLine(note.IsPinned.ToString());
        content.AppendLine(note is TaskNote task ? task.Status.ToString() : string.Empty);
        content.AppendLine(note.Tag.ToString());
        content.AppendLine(note.Format.ToString());
        content.AppendLine(ContentDelimiter);
        content.Append(note.Content);

        return content.ToString();
    }

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

    private string GetTargetPath(string? subfolderName)
    {
        return string.IsNullOrWhiteSpace(subfolderName)
            ? _folderPath
            : Path.Combine(_folderPath, subfolderName);
    }

    private IEnumerable<string> EnumerateNoteFiles(string directory)
    {
        foreach (var extension in SupportedExtensions.Keys)
        {
            var searchPattern = $"*{extension}";
            foreach (var filePath in Directory.EnumerateFiles(directory, searchPattern))
            {
                yield return filePath;
            }
        }
    }

    private static bool IsNoteTitleMatch(string filePath, string title)
    {
        using var reader = new StreamReader(filePath);
        var firstLine = reader.ReadLine();
        return firstLine == title;
    }

    private string? FindNoteFileByTitle(string directory, string title)
    {
        foreach (var filePath in EnumerateNoteFiles(directory))
        {
            if (IsNoteTitleMatch(filePath, title))
                return filePath;
        }
        return null;
    }
}
