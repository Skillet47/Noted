namespace BusinessLogic.Core;

public partial class NoteManagement
{
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

            var metadataPath = Path.Combine(TrashFolderPath, Path.GetFileName(filePath) + NoteSerializer.OriginalFolderMetadataExtension);
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
            var metadataPath = filePath + NoteSerializer.OriginalFolderMetadataExtension;

            string? originalFolder = null;

            if (File.Exists(metadataPath))
                originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);

            var destPath = GetTargetPath(originalFolder);

            Directory.CreateDirectory(destPath);
            var newPath = Path.Combine(destPath, Path.GetFileName(filePath));
            File.Move(filePath, newPath, overwrite: true);

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

            var metadataPath = filePath + NoteSerializer.OriginalFolderMetadataExtension;

            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to permanently delete note: {ex.Message}");
        }
    }
}
