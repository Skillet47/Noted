using BusinessLogic.Core.Features.Notes;
using BusinessLogic.Features.Notes;
using BusinessLogic.Models;
using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;
using System.Text;

namespace Noted.Services;

/// <summary>
/// Handles import and export operations for notes.
/// - Import: Can import individual files or entire folders. Non-Noted files are imported as GeneralNotes with metadata.
/// - Export: Saves notes to files in the Noted format.
/// </summary>
public class ImportExportService
{
    private readonly INoteManagement _noteManager;
    private const int MaxImportDepth = 10; // Prevent infinite recursion

    public ImportExportService(INoteManagement noteManager)
    {
        _noteManager = noteManager ?? throw new ArgumentNullException(nameof(noteManager));
    }

    /// <summary>
    /// Imports a single file or folder of files into the root notes folder.
    /// If a folder is selected, it creates a folder in the root and imports all notes.
    /// If a file is selected, it imports it to the root.
    /// </summary>
    /// <param name="sourceFilePath">The path to the file or folder to import</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>An operation result with status and message</returns>
    public async Task<OperationResult> ImportAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return OperationResult.Fail("Source path cannot be empty.");

        if (!File.Exists(sourceFilePath) && !Directory.Exists(sourceFilePath))
            return OperationResult.Fail("The specified file or folder does not exist.");

        try
        {
            if (File.Exists(sourceFilePath))
            {
                // Import single file to root
                return await ImportFileAsync(sourceFilePath, null, cancellationToken).ConfigureAwait(false);
            }
            else if (Directory.Exists(sourceFilePath))
            {
                // Import folder to root
                var folderName = Path.GetFileName(sourceFilePath);
                return await ImportFolderAsync(sourceFilePath, folderName, 0, cancellationToken).ConfigureAwait(false);
            }

            return OperationResult.Fail("The path is neither a file nor a folder.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Import error: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a single file to the specified subfolder (or root if null).
    /// </summary>
    private async Task<OperationResult> ImportFileAsync(string filePath, string? targetFolder, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            // Check if it's a supported note format
            bool isSupportedFormat = NoteSerializer.SupportedExtensions.ContainsKey(fileExtension);

            Note? note = null;
            string? originalFolder = null;

            if (isSupportedFormat)
            {
                // Try to read as a Noted file
                note = await NoteSerializer.ReadNoteFromFileAsync(filePath, cancellationToken).ConfigureAwait(false);
                
                if (note != null)
                {
                    originalFolder = note.OriginalFolder;
                    // It's a valid Noted file - preserve its type
                }
            }

            // If not a Noted file or couldn't parse, create a GeneralNote
            if (note == null)
            {
                var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var title = Path.GetFileNameWithoutExtension(fileName);
                
                // Ensure unique title
                var uniqueTitle = await GetUniqueNoteTitleAsync(title, targetFolder, cancellationToken).ConfigureAwait(false);

                note = new GeneralNote
                {
                    Title = uniqueTitle,
                    Content = fileContent,
                    CreatedAt = File.GetCreationTime(filePath),
                    ModifiedAt = File.GetLastWriteTime(filePath),
                    Tag = NoteTag.None,
                    Format = NoteFormat.PlainText // Default for imported files
                };

                // Store the original import folder as metadata
                originalFolder = $"Imported_{DateTimeOffset.Now:yyyyMMdd_HHmmss}";
            }

            // Save the note
            await _noteManager.SaveNoteAsync(note, targetFolder, cancellationToken).ConfigureAwait(false);

            // If there's an original folder metadata, save it
            if (!string.IsNullOrEmpty(originalFolder))
            {
                var targetPath = targetFolder == null ? _noteManager.RootFolderPath : Path.Combine(_noteManager.RootFolderPath, targetFolder);
                Directory.CreateDirectory(targetPath);

                var extension = NoteSerializer.GetFileExtension(note.Format);
                var fileName_formatted = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}{extension}";
                var filePath_full = Path.Combine(targetPath, fileName_formatted);
                var metadataPath = filePath_full + NoteSerializer.OriginalFolderMetadataExtension;

                await File.WriteAllTextAsync(metadataPath, originalFolder, cancellationToken).ConfigureAwait(false);
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to import file '{Path.GetFileName(filePath)}': {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively imports a folder and all its contents to a target folder.
    /// </summary>
    private async Task<OperationResult> ImportFolderAsync(string sourceFolder, string targetFolderName, int depth, CancellationToken cancellationToken)
    {
        if (depth > MaxImportDepth)
            return OperationResult.Fail($"Maximum folder depth exceeded. Cannot import folders nested more than {MaxImportDepth} levels deep.");

        try
        {
            // Create the target folder
            var result = _noteManager.CreateFolder(targetFolderName);
            if (!result.Success && !result.ErrorMessage?.Contains("already exists") == true)
                return result;

            // Import all files in the folder
            var filesInFolder = Directory.GetFiles(sourceFolder)
                .Where(f => !Path.GetExtension(f).Equals(".folder", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in filesInFolder)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileResult = await ImportFileAsync(file, targetFolderName, cancellationToken).ConfigureAwait(false);
                if (!fileResult.Success)
                {
                    // Log but continue with other files
                    System.Diagnostics.Debug.WriteLine($"Warning: {fileResult.ErrorMessage}");
                }
            }

            // Recursively import subfolders
            var subfolders = Directory.GetDirectories(sourceFolder);
            foreach (var subfolder in subfolders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var subfolderName = Path.GetFileName(subfolder);
                var combinedFolderName = Path.Combine(targetFolderName, subfolderName);
                var folderResult = await ImportFolderAsync(subfolder, combinedFolderName, depth + 1, cancellationToken).ConfigureAwait(false);
                if (!folderResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: {folderResult.ErrorMessage}");
                }
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to import folder '{targetFolderName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a note to a specified file path in the Noted format.
    /// </summary>
    /// <param name="note">The note to export</param>
    /// <param name="outputFilePath">The full path where the note should be saved</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>An operation result with status and message</returns>
    public async Task<OperationResult> ExportNoteAsync(Note note, string outputFilePath, CancellationToken cancellationToken = default)
    {
        if (note == null)
            return OperationResult.Fail("Note cannot be null.");

        if (string.IsNullOrWhiteSpace(outputFilePath))
            return OperationResult.Fail("Output file path cannot be empty.");

        try
        {
            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var content = NoteSerializer.BuildNoteFileContent(note);
            await File.WriteAllTextAsync(outputFilePath, content, cancellationToken).ConfigureAwait(false);

            // Export original folder metadata if present
            if (!string.IsNullOrEmpty(note.OriginalFolder))
            {
                var metadataPath = outputFilePath + NoteSerializer.OriginalFolderMetadataExtension;
                await File.WriteAllTextAsync(metadataPath, note.OriginalFolder, cancellationToken).ConfigureAwait(false);
            }

            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to export note: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures a note title is unique within the target folder by appending a number if needed.
    /// </summary>
    private async Task<string> GetUniqueNoteTitleAsync(string baseTitle, string? targetFolder, CancellationToken cancellationToken)
    {
        var existingNotes = await _noteManager.RetrieveNotesAsync(targetFolder, cancellationToken).ConfigureAwait(false);
        var existingTitles = existingNotes.Select(n => n.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingTitles.Contains(baseTitle))
            return baseTitle;

        for (int i = 1; i <= 999; i++)
        {
            var uniqueTitle = $"{baseTitle} ({i})";
            if (!existingTitles.Contains(uniqueTitle))
                return uniqueTitle;
        }

        return $"{baseTitle} ({DateTimeOffset.Now.ToUnixTimeMilliseconds()})";
    }

    /// <summary>
    /// Sanitizes a file name to make it safe for file systems.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder();

        foreach (var c in fileName)
        {
            if (!invalidChars.Contains(c) && !char.IsControl(c))
                sanitized.Append(c);
        }

        var result = sanitized.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "Note" : result;
    }
}
