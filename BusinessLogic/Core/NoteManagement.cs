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

    private static string GetFileExtension(NoteFormat format) => format switch
    {
        NoteFormat.PlainText => ".txt",
        NoteFormat.Markdown => ".md",
        NoteFormat.RichText => ".rtf",
        _ => ".txt"
    };

    private static NoteFormat GetFormatFromExtension(string extension)
    {
        return SupportedExtensions.GetValueOrDefault(extension, NoteFormat.PlainText);
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

    public Task<IEnumerable<Note>> RetrieveNotesAsync(CancellationToken cancellationToken = default)
    {
        return RetrieveNotesFromPathAsync(_folderPath, cancellationToken);
    }

    public Task<IEnumerable<Note>> RetrieveNotesAsync(string? subfolderName, CancellationToken cancellationToken = default)
    {
        var targetPath = GetTargetPath(subfolderName);
        return RetrieveNotesFromPathAsync(targetPath, cancellationToken);
    }

    private async Task<IEnumerable<Note>> RetrieveNotesFromPathAsync(string path, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
            return [];

        var notes = new List<Note>();

        foreach (var filePath in EnumerateNoteFiles(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var note = await ReadNoteFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);

            if (note is not null)
                notes.Add(note);
        }

        return notes;
    }

    private static async Task<Note?> ReadNoteFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken).ConfigureAwait(false);
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

        // Check for original folder metadata (for notes in trash)
        string? originalFolder = null;
        var metadataPath = filePath + OriginalFolderMetadataExtension;
        if (File.Exists(metadataPath))
        {
            originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(originalFolder))
                originalFolder = null;
        }

        Note note = noteType switch
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

        note.OriginalFolder = originalFolder;
        return note;
    }

    public Task<OperationResult> SaveNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        return SaveNoteAsync(note, null, cancellationToken);
    }

    public async Task<OperationResult> SaveNoteAsync(Note? note, string? subfolderName, CancellationToken cancellationToken = default)
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

            await File.WriteAllTextAsync(filePath, content, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to save note: {ex.Message}");
        }
    }

    public Task<OperationResult> DeleteNoteAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        return DeleteNoteAsync(noteTitle, null, cancellationToken);
    }

    public async Task<OperationResult> DeleteNoteAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(targetPath, noteTitle, cancellationToken).ConfigureAwait(false);

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

    public Task<OperationResult> UpdateNoteAsync(string originalTitle, Note updatedNote, CancellationToken cancellationToken = default)
    {
        return UpdateNoteAsync(originalTitle, updatedNote, null, cancellationToken);
    }

    public async Task<OperationResult> UpdateNoteAsync(string originalTitle, Note updatedNote, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(targetPath, originalTitle, cancellationToken).ConfigureAwait(false);

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

                await File.WriteAllTextAsync(newFilePath, content, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, content, cancellationToken).ConfigureAwait(false);
            }
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to update note: {ex.Message}");
        }
    }

    public async Task<OperationResult> MoveNoteToTrashAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var sourcePath = GetTargetPath(subfolderName);

        if (!Directory.Exists(sourcePath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(sourcePath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found.");

        try
        {
            Directory.CreateDirectory(TrashFolderPath);

            var destPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath));

            File.Move(filePath, destPath, overwrite: true);

            // Save original folder metadata
            var metadataPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath) + OriginalFolderMetadataExtension);
            await File.WriteAllTextAsync(metadataPath, subfolderName ?? string.Empty, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to move note to trash: {ex.Message}");
        }
    }

    public async Task<OperationResult> RestoreNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(trashPath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found in trash.");

        try
        {
            var metadataPath = filePath + OriginalFolderMetadataExtension;

            string? originalFolder = null;

            if (File.Exists(metadataPath))
            {
                originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to restore note from trash: {ex.Message}");
        }
    }

    public async Task<OperationResult> PermanentlyDeleteNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(trashPath, noteTitle, cancellationToken).ConfigureAwait(false);

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

    public async Task<OperationResult> DeleteFolderAsync(string folderName, CancellationToken cancellationToken = default)
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
                cancellationToken.ThrowIfCancellationRequested();
                var note = await ReadNoteFromFileAsync(noteFile, cancellationToken).ConfigureAwait(false);
                if (note != null)
                {
                    await MoveNoteToTrashAsync(note.Title, folderName, cancellationToken).ConfigureAwait(false);
                }
            }

            // Delete the folder and its contents
            Directory.Delete(folderPath, true);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
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

    private static async Task<bool> IsNoteTitleMatchAsync(string filePath, string title, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        var firstLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return firstLine == title;
    }

    private async Task<string?> FindNoteFileByTitleAsync(string directory, string title, CancellationToken cancellationToken)
    {
        foreach (var filePath in EnumerateNoteFiles(directory))
        {
            if (await IsNoteTitleMatchAsync(filePath, title, cancellationToken).ConfigureAwait(false))
                return filePath;
        }
        return null;
    }
}
