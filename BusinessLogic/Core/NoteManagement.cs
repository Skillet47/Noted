using BusinessLogic.Notes;
using System.Text;

namespace BusinessLogic.Core;

/// <summary>
/// Provides core note management functionality, including creating, retrieving, updating, and deleting notes and folders.
/// Supports multiple note formats and subfolder organization.
/// </summary>
public class NoteManagement(string folderPath)
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

    public bool CreateFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return false;

        var sanitizedName = SanitizeFileName(folderName);
        var newFolderPath = Path.Combine(_folderPath, sanitizedName);

        if (Directory.Exists(newFolderPath))
            return false;

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

    public void SaveNote(Note note)
    {
        if (string.IsNullOrWhiteSpace(note?.Title))
            return;

        SaveNote(note, null);
    }

    public void SaveNote(Note note, string? subfolderName)
    {
        if (string.IsNullOrWhiteSpace(note?.Title))
            return;

        var targetPath = GetTargetPath(subfolderName);

        Directory.CreateDirectory(targetPath);

        var extension = GetFileExtension(note.Format);
        var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(targetPath, fileName);
        var content = BuildNoteFileContent(note);

        File.WriteAllText(filePath, content);
    }

    public bool DeleteNote(string noteTitle)
    {
        return DeleteNote(noteTitle, null);
    }

    public bool DeleteNote(string noteTitle, string? subfolderName)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return false;

        var filePath = FindNoteFileByTitle(targetPath, noteTitle);

        if (filePath is null)
            return false;

        File.Delete(filePath);

        return true;
    }

    public bool UpdateNote(string originalTitle, Note updatedNote)
    {
        return UpdateNote(originalTitle, updatedNote, null);
    }

    public bool UpdateNote(string originalTitle, Note updatedNote, string? subfolderName)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return false;

        var filePath = FindNoteFileByTitle(targetPath, originalTitle);

        if (filePath is null)
            return false;

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
        return true;
    }

    public bool MoveNoteToTrash(string noteTitle, string? subfolderName)
    {
        var sourcePath = GetTargetPath(subfolderName);

        if (!Directory.Exists(sourcePath))
            return false;

        var filePath = FindNoteFileByTitle(sourcePath, noteTitle);

        if (filePath is null)
            return false;

        Directory.CreateDirectory(TrashFolderPath);

        var destPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath));

        File.Move(filePath, destPath, overwrite: true);

        // Save original folder metadata
        var metadataPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath) + OriginalFolderMetadataExtension);
        File.WriteAllText(metadataPath, subfolderName ?? string.Empty);
        return true;
    }

    public bool RestoreNoteFromTrash(string noteTitle)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return false;

        var filePath = FindNoteFileByTitle(trashPath, noteTitle);

        if (filePath is null)
            return false;

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
        return true;
    }

    public bool PermanentlyDeleteNoteFromTrash(string noteTitle)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return false;

        var filePath = FindNoteFileByTitle(trashPath, noteTitle);

        if (filePath is null)
            return false;

        File.Delete(filePath);

        var metadataPath = filePath + OriginalFolderMetadataExtension;

        if (File.Exists(metadataPath))
            File.Delete(metadataPath);
        return true;
    }

    public bool DeleteFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName) || folderName == TrashFolderName || folderName == string.Empty)
            return false;

        var folderPath = Path.Combine(_folderPath, folderName);
        if (!Directory.Exists(folderPath))
            return false;

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
        try
        {
            Directory.Delete(folderPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildNoteFileContent(Note note)
    {
        var content = new StringBuilder();

        content.AppendLine(note.Title);
        content.AppendLine(note.CreatedAt.ToString("O"));
        content.AppendLine(note.ModifiedAt.ToString("O"));
        content.AppendLine(note.Type.ToString());
        content.AppendLine(note is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);
        content.AppendLine(note is ReminderNote reminder2 ? reminder2.Recurrence.ToString() : string.Empty);
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
        var lines = File.ReadAllLines(filePath);
        return lines.Length > 0 && lines[0] == title;
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
