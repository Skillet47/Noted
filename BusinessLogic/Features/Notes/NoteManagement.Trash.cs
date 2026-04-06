using BusinessLogic.Features.Notes;
using BusinessLogic.Models;

namespace BusinessLogic.Core.Features.Notes;

public partial class NoteManagement
{
    public async Task<OperationResult> MoveNoteToTrashAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(noteTitle))
            return OperationResult.Fail("Note title cannot be empty.");

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
            MoveHistoryFile(filePath, destPath);

            try
            {
                var metadataPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath) + NoteSerializer.OriginalFolderMetadataExtension);
                await File.WriteAllTextAsync(metadataPath, subfolderName ?? string.Empty, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to write original folder metadata - {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: No permission to write original folder metadata - {ex.Message}");
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return OperationResult.Fail($"Failed to move note to trash: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Fail($"No permission to move note to trash: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to move note to trash: {ex.Message}");
        }
    }

    public async Task<OperationResult> RestoreNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(noteTitle))
            return OperationResult.Fail("Note title cannot be empty.");

        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(trashPath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found in trash.");

        try
        {
            var metadataPath = filePath + NoteSerializer.OriginalFolderMetadataExtension;

            string? originalFolder = null;

            if (File.Exists(metadataPath))
            {
                try
                {
                    originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(originalFolder))
                        originalFolder = null;
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to read original folder metadata - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No permission to read original folder metadata - {ex.Message}");
                }
            }

            var destPath = GetTargetPath(originalFolder);

            Directory.CreateDirectory(destPath);
            var newPath = Path.Combine(destPath, Path.GetFileName(filePath));
            File.Move(filePath, newPath, overwrite: true);
            MoveHistoryFile(filePath, newPath);

            if (File.Exists(metadataPath))
            {
                try
                {
                    File.Delete(metadataPath);
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete original folder metadata - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No permission to delete original folder metadata - {ex.Message}");
                }
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return OperationResult.Fail($"Failed to restore note from trash: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Fail($"No permission to restore note from trash: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to restore note from trash: {ex.Message}");
        }
    }

    public async Task<OperationResult> PermanentlyDeleteNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(noteTitle))
            return OperationResult.Fail("Note title cannot be empty.");

        var trashPath = TrashFolderPath;

        if (!Directory.Exists(trashPath))
            return OperationResult.Fail("Trash folder does not exist.");

        var filePath = await FindNoteFileByTitleAsync(trashPath, noteTitle, cancellationToken).ConfigureAwait(false);

        if (filePath is null)
            return OperationResult.Fail($"Note '{noteTitle}' not found in trash.");

        try
        {
            File.Delete(filePath);
            DeleteHistoryFile(filePath);

            var metadataPath = filePath + NoteSerializer.OriginalFolderMetadataExtension;

            if (File.Exists(metadataPath))
            {
                try
                {
                    File.Delete(metadataPath);
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete original folder metadata - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No permission to delete original folder metadata - {ex.Message}");
                }
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return OperationResult.Fail($"Failed to permanently delete note: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Fail($"No permission to permanently delete note: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to permanently delete note: {ex.Message}");
        }
    }
}
