namespace Noted.Services;

/// <summary>
/// Manages the storage location for note files.
/// Allows users to customize where their notes are saved on the file system.
/// </summary>
/// <remarks>
/// <para>
/// The storage location is persisted using MAUI Preferences, which provides
/// platform-appropriate storage for application settings.
/// </para>
/// <para>
/// <b>Important:</b> Changing the storage location requires an app restart
/// because the <see cref="BusinessLogic.Core.NoteManagement"/> instance is created
/// at startup with the initial storage path.
/// </para>
/// </remarks>
public class StorageService
{
    private const string StorageLocationKey = "NotesStorageLocation";
    private const string CurrentFolderKey = "NotesCurrentFolder";
    private readonly string _defaultLocation;

    /// <summary>
    /// Event raised when the storage location is changed.
    /// Subscribers should handle this to update any cached paths.
    /// </summary>
    public event Action? OnStorageLocationChanged;

    /// <summary>
    /// Event raised when the current folder is changed.
    /// Subscribers should handle this to refresh the notes list.
    /// </summary>
    public event Action? OnCurrentFolderChanged;

    public StorageService()
    {
        // Default to the app's data directory, which is platform-appropriate
        _defaultLocation = Path.Combine(FileSystem.AppDataDirectory, "Notes");
    }

    /// <summary>
    /// Gets or sets the current storage location for note files.
    /// </summary>
    /// <remarks>
    /// Setting a new value persists it to preferences and raises the
    /// <see cref="OnStorageLocationChanged"/> event.
    /// </remarks>
    public string CurrentStorageLocation
    {
        get => Preferences.Get(StorageLocationKey, _defaultLocation);
        set
        {
            if (CurrentStorageLocation != value)
            {
                Preferences.Set(StorageLocationKey, value);
                OnStorageLocationChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently selected folder within the storage location.
    /// An empty string represents the root folder.
    /// </summary>
    public string CurrentFolder
    {
        get => Preferences.Get(CurrentFolderKey, string.Empty);
        set
        {
            if (CurrentFolder != value)
            {
                Preferences.Set(CurrentFolderKey, value);
                OnCurrentFolderChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets the default storage location (app data directory).
    /// </summary>
    public string DefaultStorageLocation => _defaultLocation;

    /// <summary>
    /// Gets whether the current location is the default location.
    /// </summary>
    public bool IsDefaultLocation => CurrentStorageLocation == _defaultLocation;

    /// <summary>
    /// Resets the storage location to the default app data directory.
    /// </summary>
    public void ResetToDefault()
    {
        CurrentStorageLocation = _defaultLocation;
    }

    /// <summary>
    /// Resets the current folder to the root folder.
    /// </summary>
    public void ResetToRootFolder()
    {
        CurrentFolder = string.Empty;
    }

    /// <summary>
    /// Validates whether a given path is a valid file system path.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    public bool IsValidPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return !string.IsNullOrWhiteSpace(fullPath);
        }
        catch
        {
            return false;
        }
    }
}
