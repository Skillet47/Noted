namespace Noted.Services;

/// <summary>
/// Provides native file and folder picker dialogs for selecting notes and folders to import/export.
/// Platform-specific implementations are provided via conditional compilation.
/// </summary>
public interface IFilesPickerService
{
    /// <summary>
    /// Opens a native folder picker and returns the selected path,
    /// or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> PickFolderAsync();

    /// <summary>
    /// Opens a native file picker and returns the selected file path,
    /// or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> PickFileAsync();
}
