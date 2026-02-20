namespace BusinessLogic.Core;

/// <summary>
/// Defines the contract for note management operations including CRUD operations for notes and folders.
/// </summary>
public interface INoteManagement
{
    /// <summary>
    /// Gets the root folder path where notes are stored.
    /// </summary>
    string RootFolderPath { get; }

    /// <summary>
    /// Gets the path to the trash folder.
    /// </summary>
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
