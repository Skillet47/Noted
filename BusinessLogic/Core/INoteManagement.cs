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
    /// Retrieves all notes from the root folder.
    /// </summary>
    IEnumerable<Note> RetrieveNotes();

    /// <summary>
    /// Retrieves all notes from a specific subfolder.
    /// </summary>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    IEnumerable<Note> RetrieveNotes(string? subfolderName);

    /// <summary>
    /// Saves a note to the root folder.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult SaveNote(Note note);

    /// <summary>
    /// Saves a note to a specific subfolder.
    /// </summary>
    /// <param name="note">The note to save.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult SaveNote(Note note, string? subfolderName);

    /// <summary>
    /// Deletes a note by title from the root folder.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult DeleteNote(string noteTitle);

    /// <summary>
    /// Deletes a note by title from a specific subfolder.
    /// </summary>
    /// <param name="noteTitle">The title of the note to delete.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult DeleteNote(string noteTitle, string? subfolderName);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update.</param>
    /// <param name="updatedNote">The updated note data.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult UpdateNote(string originalTitle, Note updatedNote);

    /// <summary>
    /// Updates an existing note in a specific subfolder.
    /// </summary>
    /// <param name="originalTitle">The original title of the note to update.</param>
    /// <param name="updatedNote">The updated note data.</param>
    /// <param name="subfolderName">The name of the subfolder, or null for the root folder.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult UpdateNote(string originalTitle, Note updatedNote, string? subfolderName);

    /// <summary>
    /// Moves a note to the trash folder.
    /// </summary>
    /// <param name="noteTitle">The title of the note to move.</param>
    /// <param name="subfolderName">The name of the source subfolder, or null for the root folder.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult MoveNoteToTrash(string noteTitle, string? subfolderName);

    /// <summary>
    /// Restores a note from the trash folder to its original location.
    /// </summary>
    /// <param name="noteTitle">The title of the note to restore.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult RestoreNoteFromTrash(string noteTitle);

    /// <summary>
    /// Permanently deletes a note from the trash folder.
    /// </summary>
    /// <param name="noteTitle">The title of the note to permanently delete.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult PermanentlyDeleteNoteFromTrash(string noteTitle);

    /// <summary>
    /// Creates a new subfolder.
    /// </summary>
    /// <param name="folderName">The name of the folder to create.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult CreateFolder(string folderName);

    /// <summary>
    /// Deletes a subfolder, moving its notes to the trash.
    /// </summary>
    /// <param name="folderName">The name of the folder to delete.</param>
    /// <returns>An operation result indicating success or failure.</returns>
    OperationResult DeleteFolder(string folderName);

    /// <summary>
    /// Gets all subfolder names in the root folder.
    /// </summary>
    IEnumerable<string> GetSubfolders();
}
