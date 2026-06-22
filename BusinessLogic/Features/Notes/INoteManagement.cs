using BusinessLogic.Models;
using BusinessLogic.Models.Notes;

namespace BusinessLogic.Features.Notes;

/// <summary>
/// Defines the contract for note management operations including CRUD operations for notes and folders.
/// </summary>
public interface INoteManagement
{
    string RootFolderPath { get; }
    string TrashFolderPath { get; }

    /// <summary>
    /// Retrieves all notes from the root folder asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<IEnumerable<Note>> RetrieveNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all notes from a specific subfolder asynchronously.
    /// </summary>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<IEnumerable<Note>> RetrieveNotesAsync(string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a note to the root folder asynchronously.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> SaveNoteAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a note to a specific subfolder asynchronously.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> SaveNoteAsync(Note note, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note by title from the root folder asynchronously.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> DeleteNoteAsync(string noteTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note by title from a specific subfolder asynchronously.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> DeleteNoteAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing note asynchronously.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update.</param>
    /// <param name="updatedNote">The updated note data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> UpdateNoteAsync(string originalTitle, Note updatedNote, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing note in a specific subfolder asynchronously.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update.</param>
    /// <param name="updatedNote">The updated note data.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> UpdateNoteAsync(string originalTitle, Note updatedNote, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the physical file path for a note in the root folder.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The note file path if found; otherwise, null.</returns>
    Task<string?> GetNoteFilePathAsync(string noteTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the physical file path for a note in a specific subfolder.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The note file path if found; otherwise, null.</returns>
    Task<string?> GetNoteFilePathAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves change history for a note in the root folder.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<IReadOnlyList<NoteHistoryEntry>> GetNoteHistoryAsync(string noteTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves change history for a note in a specific subfolder.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<IReadOnlyList<NoteHistoryEntry>> GetNoteHistoryAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts a note in the root folder to a specific history snapshot.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="changedAtUtc">The snapshot timestamp from note history.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<OperationResult> RevertNoteToHistoryAsync(string noteTitle, DateTime changedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts a note in a specific subfolder to a specific history snapshot.
    /// </summary>
    /// <param name="noteTitle">The title of the note.</param>
    /// <param name="changedAtUtc">The snapshot timestamp from note history.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task<OperationResult> RevertNoteToHistoryAsync(string noteTitle, DateTime changedAtUtc, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a note from one folder to another.
    /// </summary>
    /// <param name="noteTitle">The title of the note to move.</param>
    /// <param name="sourceFolder">The source folder name, or null/empty for root.</param>
    /// <param name="destinationFolder">The destination folder name, or null/empty for root.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> MoveNoteAsync(string noteTitle, string? sourceFolder, string? destinationFolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a note to the trash folder asynchronously.
    /// </summary>
    /// <param name="noteTitle">The title of the note to move.</param>
    /// <param name="subfolderName">The name of the source subfolder, or null for the root folder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> MoveNoteToTrashAsync(string noteTitle, string? subfolderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a note from the trash folder to its original location asynchronously.
    /// </summary>
    /// <param name="noteTitle">The title of the note to restore.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> RestoreNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a note from the trash folder asynchronously.
    /// </summary>
    /// <param name="noteTitle">The title of the note to permanently delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> PermanentlyDeleteNoteFromTrashAsync(string noteTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new subfolder.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult CreateFolder(string folderName);

    /// <summary>
    /// Deletes a subfolder, moving its notes to the trash asynchronously.
    /// </summary>
    /// <param name="folderName">The name of the folder to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    Task<OperationResult> DeleteFolderAsync(string folderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subfolder names in the root folder.
    /// </summary>
    IEnumerable<string> GetSubfolders();
}
