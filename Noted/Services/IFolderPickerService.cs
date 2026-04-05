namespace Noted.Services;

/// <summary>
/// Provides a native folder-picker dialog for selecting a storage location.
/// Platform-specific implementations are provided via conditional compilation in
/// <see cref="FolderPickerService"/>.
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// Opens a native folder picker and returns the selected path,
    /// or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> PickFolderAsync();
}
