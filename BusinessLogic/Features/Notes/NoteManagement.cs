using System.Text;
using System.Text.Json;
using BusinessLogic.Features.Notes;
using BusinessLogic.Models;
using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;

namespace BusinessLogic.Core.Features.Notes;

/// <summary>
/// Provides core note management functionality, including creating, retrieving, updating, and deleting notes and folders.
/// Supports multiple note formats and subfolder organization.
/// </summary>
public partial class NoteManagement : INoteManagement
{
    private readonly Func<string> _rootPathProvider;
    private const string TrashFolderName = "Trash";
    private const string HistoryMetadataExtension = ".history.json";
    private static readonly JsonSerializerOptions HistorySerializerOptions = new() { WriteIndented = false };
    private const int MaxHistoryEntries = 50;
    private readonly SemaphoreSlim _metadataInitializationLock = new(1, 1);
    private string? _initializedMetadataRoot;

    public NoteManagement(string folderPath)
        : this(() => folderPath)
    {
    }

    public NoteManagement(Func<string> rootPathProvider)
    {
        _rootPathProvider = rootPathProvider ?? throw new ArgumentNullException(nameof(rootPathProvider));
    }

    public string RootFolderPath => _rootPathProvider();
    public string TrashFolderPath => Path.Combine(RootFolderPath, TrashFolderName);

    public Task<IEnumerable<Note>> RetrieveNotesAsync(CancellationToken cancellationToken = default)
    {
        return RetrieveNotesFromPathAsync(RootFolderPath, cancellationToken);
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

        var metadataStore = await GetMetadataStoreAsync(cancellationToken).ConfigureAwait(false);
        var relativeFolder = GetRelativePath(path);
        var notes = new List<Note>();

        foreach (var metadata in await metadataStore.ListAsync(relativeFolder, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = Path.Combine(RootFolderPath, metadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(filePath) || !string.Equals(Path.GetDirectoryName(filePath), path, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var note = await ReadManagedNoteAsync(filePath, metadata, cancellationToken).ConfigureAwait(false);
                if (note is not null)
                    notes.Add(note);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Skip malformed or inaccessible files instead of failing the whole listing.
                continue;
            }

        }

        return notes;
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
            var metadataStore = await GetMetadataStoreAsync(cancellationToken).ConfigureAwait(false);
            var targetPath = GetTargetPath(subfolderName);

            Directory.CreateDirectory(targetPath);

            var extension = NoteSerializer.GetFileExtension(note.Format);
            var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(targetPath, fileName);
            await File.WriteAllTextAsync(filePath, note.Content, cancellationToken).ConfigureAwait(false);
            await metadataStore.UpsertAsync(ToMetadata(note, GetRelativePath(filePath)), cancellationToken).ConfigureAwait(false);
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
        var metadataStore = await GetMetadataStoreAsync(cancellationToken).ConfigureAwait(false);
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(targetPath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found.");

        try
        {
            File.Delete(filePath);
            DeleteHistoryFile(filePath);
            await metadataStore.DeleteAsync(GetRelativePath(filePath), cancellationToken).ConfigureAwait(false);
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

    public Task<string?> GetNoteFilePathAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        return GetNoteFilePathAsync(noteTitle, null, cancellationToken);
    }

    public async Task<string?> GetNoteFilePathAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(noteTitle))
            return null;

        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return null;

        return await FindNoteFileByTitleAsync(targetPath, noteTitle, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<NoteHistoryEntry>> GetNoteHistoryAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        return GetNoteHistoryAsync(noteTitle, null, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteHistoryEntry>> GetNoteHistoryAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return [];

        var notePath = await FindNoteFileByTitleAsync(targetPath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (notePath is null)
            return [];

        return await ReadHistoryEntriesAsync(notePath, cancellationToken).ConfigureAwait(false);
    }

    public Task<OperationResult> RevertNoteToHistoryAsync(string noteTitle, DateTime changedAtUtc, CancellationToken cancellationToken = default)
    {
        return RevertNoteToHistoryAsync(noteTitle, changedAtUtc, null, cancellationToken);
    }

    public async Task<OperationResult> RevertNoteToHistoryAsync(string noteTitle, DateTime changedAtUtc, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail("Folder does not exist.");

        var notePath = await FindNoteFileByTitleAsync(targetPath, noteTitle, cancellationToken).ConfigureAwait(false);
        if (notePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found.");

        var historyEntries = await ReadHistoryEntriesAsync(notePath, cancellationToken).ConfigureAwait(false);
        var historyEntry = historyEntries.FirstOrDefault(entry => entry.ChangedAtUtc == changedAtUtc);
        if (historyEntry is null)
            return OperationResult.Fail("History entry was not found for this note.");

        var revertedNote = historyEntry.ToNote();
        return await UpdateNoteAsync(noteTitle, revertedNote, subfolderName, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OperationResult> UpdateNoteAsync(string originalTitle, Note updatedNote, string? subfolderName, CancellationToken cancellationToken = default)
    {
        var metadataStore = await GetMetadataStoreAsync(cancellationToken).ConfigureAwait(false);
        var targetPath = GetTargetPath(subfolderName);

        if (!Directory.Exists(targetPath))
            return OperationResult.Fail($"Folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(targetPath, originalTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{originalTitle}' not found.");

        try
        {
            var existingMetadata = await metadataStore.GetAsync(GetRelativePath(filePath), cancellationToken).ConfigureAwait(false);
            var existingNote = existingMetadata is null ? null : await ReadManagedNoteAsync(filePath, existingMetadata, cancellationToken).ConfigureAwait(false);
            if (existingNote is null)
                return OperationResult.Fail("Failed to read existing note for update.");

            var historyEntry = NoteHistoryEntry.FromNote(existingNote, DateTime.UtcNow);

            updatedNote.ModifiedAt = DateTime.Now;

            var currentExtension = Path.GetExtension(filePath);
            var newExtension = NoteSerializer.GetFileExtension(updatedNote.Format);
            string historyTargetPath;

            if (!currentExtension.Equals(newExtension, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(filePath);

                var newFileName = Path.GetFileNameWithoutExtension(filePath) + newExtension;
                var newFilePath = Path.Combine(targetPath, newFileName);

                await File.WriteAllTextAsync(newFilePath, updatedNote.Content, cancellationToken).ConfigureAwait(false);
                MoveHistoryFile(filePath, newFilePath);
                await metadataStore.DeleteAsync(GetRelativePath(filePath), cancellationToken).ConfigureAwait(false);
                historyTargetPath = newFilePath;
            }
            else
            {
                await File.WriteAllTextAsync(filePath, updatedNote.Content, cancellationToken).ConfigureAwait(false);
                historyTargetPath = filePath;
            }

            await metadataStore.UpsertAsync(ToMetadata(updatedNote, GetRelativePath(historyTargetPath)), cancellationToken).ConfigureAwait(false);
            await AppendHistoryEntryAsync(historyTargetPath, historyEntry, cancellationToken).ConfigureAwait(false);
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
            ? RootFolderPath
            : Path.Combine(RootFolderPath, subfolderName);
    }

    private IEnumerable<string> EnumerateNoteFiles(string directory)
    {
        var files = new List<string>();

        foreach (var extension in NoteSerializer.SupportedExtensions.Keys)
        {
            var searchPattern = $"*{extension}";
            try
            {
                files.AddRange(Directory.EnumerateFiles(directory, searchPattern));
            }
            catch (IOException)
            {
                // Ignore transient IO failures for this extension and continue.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore folders/files we cannot access and continue.
            }
        }

        return files;
    }

    private async Task<string?> FindNoteFileByTitleAsync(string directory, string title, CancellationToken cancellationToken)
    {
        var store = await GetMetadataStoreAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await store.FindByTitleAsync(GetRelativePath(directory), title, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
            return null;

        var filePath = Path.Combine(RootFolderPath, metadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(filePath) && string.Equals(Path.GetDirectoryName(filePath), directory, StringComparison.OrdinalIgnoreCase)
            ? filePath
            : null;
    }

    private async Task<NoteMetadataStore> GetMetadataStoreAsync(CancellationToken cancellationToken)
    {
        var rootPath = RootFolderPath;
        await _metadataInitializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = new NoteMetadataStore(rootPath);
            if (!string.Equals(_initializedMetadataRoot, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                await store.InitializeAsync(cancellationToken).ConfigureAwait(false);
                await MigrateLegacyNotesAsync(store, cancellationToken).ConfigureAwait(false);
                _initializedMetadataRoot = rootPath;
            }
            return store;
        }
        finally
        {
            _metadataInitializationLock.Release();
        }
    }

    private async Task MigrateLegacyNotesAsync(NoteMetadataStore store, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(RootFolderPath))
            return;

        foreach (var filePath in Directory.EnumerateFiles(RootFolderPath, "*.*", SearchOption.AllDirectories)
                     .Where(path => NoteSerializer.SupportedExtensions.ContainsKey(Path.GetExtension(path)) &&
                                    !path.Contains(Path.Combine(RootFolderPath, ".noted"), StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = GetRelativePath(filePath);
            if (await store.GetAsync(relativePath, cancellationToken).ConfigureAwait(false) is not null)
                continue;

            var legacyNote = await NoteSerializer.ReadNoteFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (legacyNote is null)
                continue;

            await File.WriteAllTextAsync(filePath, legacyNote.Content, cancellationToken).ConfigureAwait(false);
            await store.UpsertAsync(ToMetadata(legacyNote, relativePath), cancellationToken).ConfigureAwait(false);
        }
    }

    private string GetRelativePath(string path)
    {
        var relativePath = Path.GetRelativePath(RootFolderPath, path);
        return relativePath == "." ? string.Empty : relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static NoteMetadata ToMetadata(Note note, string relativePath) => new()
    {
        RelativePath = relativePath,
        Title = note.Title,
        CreatedAt = note.CreatedAt,
        ModifiedAt = note.ModifiedAt,
        Type = note.Type,
        IsPinned = note.IsPinned,
        Tag = note.Tag,
        Format = note.Format,
        ReminderDateTime = note is ReminderNote reminder ? reminder.ReminderDateTime : DateTime.MinValue,
        Recurrence = note is ReminderNote reminderNote ? reminderNote.Recurrence : RecurrencePattern.None,
        TaskStatus = note is TaskNote task ? task.Status : NoteTaskStatus.NotStarted,
        IdeaStage = note is IdeaNote idea ? idea.Stage : IdeaStage.Seed,
        OriginalFolder = note.OriginalFolder
    };

    private static async Task<Note?> ReadManagedNoteAsync(string filePath, NoteMetadata metadata, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return metadata.Type switch
        {
            NoteType.General => new GeneralNote { Title = metadata.Title, Content = content, CreatedAt = metadata.CreatedAt, ModifiedAt = metadata.ModifiedAt, IsPinned = metadata.IsPinned, Tag = metadata.Tag, Format = metadata.Format, OriginalFolder = metadata.OriginalFolder },
            NoteType.Reminder => new ReminderNote { Title = metadata.Title, Content = content, CreatedAt = metadata.CreatedAt, ModifiedAt = metadata.ModifiedAt, IsPinned = metadata.IsPinned, Tag = metadata.Tag, Format = metadata.Format, ReminderDateTime = metadata.ReminderDateTime, Recurrence = metadata.Recurrence, OriginalFolder = metadata.OriginalFolder },
            NoteType.Task => new TaskNote { Title = metadata.Title, Content = content, CreatedAt = metadata.CreatedAt, ModifiedAt = metadata.ModifiedAt, IsPinned = metadata.IsPinned, Tag = metadata.Tag, Format = metadata.Format, Status = metadata.TaskStatus, OriginalFolder = metadata.OriginalFolder },
            NoteType.Idea => new IdeaNote { Title = metadata.Title, Content = content, CreatedAt = metadata.CreatedAt, ModifiedAt = metadata.ModifiedAt, IsPinned = metadata.IsPinned, Tag = metadata.Tag, Format = metadata.Format, Stage = metadata.IdeaStage, OriginalFolder = metadata.OriginalFolder },
            _ => null
        };
    }

    private static string GetHistoryPath(string noteFilePath)
    {
        return noteFilePath + HistoryMetadataExtension;
    }

    private async Task AppendHistoryEntryAsync(string noteFilePath, NoteHistoryEntry historyEntry, CancellationToken cancellationToken)
    {
        try
        {
            var historyEntries = await ReadHistoryEntriesAsync(noteFilePath, cancellationToken).ConfigureAwait(false);
            var updatedHistoryEntries = historyEntries.ToList();
            updatedHistoryEntries.Add(historyEntry);

            if (updatedHistoryEntries.Count > MaxHistoryEntries)
                updatedHistoryEntries = updatedHistoryEntries
                    .OrderBy(e => e.ChangedAtUtc)
                    .TakeLast(MaxHistoryEntries)
                    .ToList();

            var json = JsonSerializer.Serialize(updatedHistoryEntries, HistorySerializerOptions);
            await File.WriteAllTextAsync(GetHistoryPath(noteFilePath), json, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            // History serialization failed, but don't fail the main operation
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to serialize history: {ex.Message}");
        }
        catch (IOException ex)
        {
            // File I/O error, continue without failing the operation
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to write history file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied, continue gracefully
            System.Diagnostics.Debug.WriteLine($"Warning: No permission to write history file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Other unexpected errors
            System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error appending history entry: {ex.Message}");
        }
    }

    private static void MoveHistoryFile(string oldNotePath, string newNotePath)
    {
        try
        {
            var oldHistoryPath = GetHistoryPath(oldNotePath);
            if (!File.Exists(oldHistoryPath))
                return;

            var newHistoryPath = GetHistoryPath(newNotePath);
            File.Move(oldHistoryPath, newHistoryPath, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            // History file move failed, continue without failing the operation
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to move history file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied for history file
            System.Diagnostics.Debug.WriteLine($"Warning: No permission to move history file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Other unexpected errors
            System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error moving history file: {ex.Message}");
        }
    }

    private static void DeleteHistoryFile(string noteFilePath)
    {
        try
        {
            var historyPath = GetHistoryPath(noteFilePath);
            if (File.Exists(historyPath))
                File.Delete(historyPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            // History file deletion failed, continue without failing
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete history file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied for history file
            System.Diagnostics.Debug.WriteLine($"Warning: No permission to delete history file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Other unexpected errors
            System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error deleting history file: {ex.Message}");
        }
    }

    private static async Task<IReadOnlyList<NoteHistoryEntry>> ReadHistoryEntriesAsync(string noteFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var historyPath = GetHistoryPath(noteFilePath);
            if (!File.Exists(historyPath))
                return [];

            var json = await File.ReadAllTextAsync(historyPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            var entries = JsonSerializer.Deserialize<List<NoteHistoryEntry>>(json, HistorySerializerOptions);
            return entries ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            // History file is malformed, return empty list
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to deserialize history file - corrupted format: {ex.Message}");
            return [];
        }
        catch (IOException ex)
        {
            // File I/O error, return empty list
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to read history file: {ex.Message}");
            return [];
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied, return empty list
            System.Diagnostics.Debug.WriteLine($"Warning: No permission to read history file: {ex.Message}");
            return [];
        }
        catch (Exception ex)
        {
            // Other unexpected errors
            System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error reading history: {ex.Message}");
            return [];
        }
    }
}
