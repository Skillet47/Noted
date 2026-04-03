using BusinessLogic.Features.Notes;
using BusinessLogic.Models;

namespace BusinessLogic.Core.Features.Notes;

public partial class NoteManagement
{
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
            foreach (var noteFile in EnumerateNoteFiles(folderPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var note = await NoteSerializer.ReadNoteFromFileAsync(noteFile, cancellationToken).ConfigureAwait(false);
                if (note != null)
                    await MoveNoteToTrashAsync(note.Title, folderName, cancellationToken).ConfigureAwait(false);
            }

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
}
