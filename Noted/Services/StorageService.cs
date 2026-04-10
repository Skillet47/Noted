using BusinessLogic.Shared;
#if MACCATALYST
using Foundation;
#endif

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
/// Changing the storage location takes effect immediately for note operations.
/// </para>
/// </remarks>
public class StorageService
{
    private const string StorageLocationKey = "NotesStorageLocation";
#if MACCATALYST
    private const string StorageLocationBookmarkKey = "NotesStorageLocationBookmark";
    private NSUrl? _activeSecurityScopeUrl;
#endif
    private const string CurrentFolderKey = "NotesCurrentFolder";
    private const string PreviewSizeKey = "NotesPreviewSize";
    private readonly string _defaultLocation = Path.Combine(FileSystem.AppDataDirectory, "Notes");

    public StorageService()
    {
#if MACCATALYST
        RestoreSecurityScopeAccess();
#endif
        EnsureStorageLocationReady();
    }

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

    /// <summary>
    /// Event raised when the preview size is changed.
    /// Subscribers should handle this to refresh the notes display.
    /// </summary>
    public event Action? OnPreviewSizeChanged;

    // Default to the app's data directory, which is platform-appropriate

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
            var fullPath = Path.GetFullPath(value);

            if (string.Equals(CurrentStorageLocation, fullPath, StringComparison.Ordinal))
                return;

            Directory.CreateDirectory(fullPath);

            Preferences.Set(StorageLocationKey, fullPath);
            CurrentFolder = string.Empty;
            OnStorageLocationChanged?.Invoke();
        }
    }

    private void EnsureStorageLocationReady()
    {
        var current = Preferences.Get(StorageLocationKey, _defaultLocation);

        if (string.IsNullOrWhiteSpace(current))
        {
            Preferences.Set(StorageLocationKey, _defaultLocation);
            Directory.CreateDirectory(_defaultLocation);
            return;
        }

        var fullPath = Path.GetFullPath(current);

        if (!string.Equals(current, fullPath, StringComparison.Ordinal))
            Preferences.Set(StorageLocationKey, fullPath);

        Directory.CreateDirectory(fullPath);
    }

#if MACCATALYST
    private void PersistSecurityScopeBookmark(NSUrl folderUrl)
    {
        try
        {
            var bookmarkData = folderUrl.CreateBookmarkData(
#pragma warning disable CA1416
                NSUrlBookmarkCreationOptions.WithSecurityScope,
#pragma warning restore CA1416
                null,
                null,
                out var createError);

            if (bookmarkData is null || createError is not null)
                return;

            Preferences.Set(StorageLocationBookmarkKey, Convert.ToBase64String(bookmarkData.ToArray()));
            BeginSecurityScope(folderUrl);
        }
        catch
        {
            // If bookmark creation fails, continue with best-effort path usage.
        }
    }

    private void RestoreSecurityScopeAccess()
    {
        var bookmarkBase64 = Preferences.Get(StorageLocationBookmarkKey, string.Empty);
        if (string.IsNullOrWhiteSpace(bookmarkBase64))
            return;

        try
        {
            var bookmarkBytes = Convert.FromBase64String(bookmarkBase64);
            var bookmarkData = NSData.FromArray(bookmarkBytes);

            var resolvedUrl = NSUrl.FromBookmarkData(
                bookmarkData,
#pragma warning disable CA1416
                NSUrlBookmarkResolutionOptions.WithSecurityScope,
#pragma warning restore CA1416
                null,
                out var isStale,
                out var resolveError);

            if (resolvedUrl is null || resolveError is not null)
                return;

            BeginSecurityScope(resolvedUrl);

            if (isStale)
                PersistSecurityScopeBookmark(resolvedUrl);
        }
        catch
        {
            // Ignore corrupted bookmark data and continue startup.
        }
    }

    private void BeginSecurityScope(NSUrl url)
    {
        if (_activeSecurityScopeUrl is not null)
            _activeSecurityScopeUrl.StopAccessingSecurityScopedResource();

        if (url.StartAccessingSecurityScopedResource())
            _activeSecurityScopeUrl = url;
    }
#endif

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
#if MACCATALYST
        if (_activeSecurityScopeUrl is not null)
        {
            _activeSecurityScopeUrl.StopAccessingSecurityScopedResource();
            _activeSecurityScopeUrl = null;
        }

        Preferences.Remove(StorageLocationBookmarkKey);
#endif
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

    /// <summary>
    /// Gets or sets the preview size for note content in the notes list.
    /// </summary>
    public PreviewSize PreviewSize
    {
        get
        {
            var value = Preferences.Get(PreviewSizeKey, nameof(PreviewSize.Medium));
            return Enum.TryParse<PreviewSize>(value, out var size) ? size : PreviewSize.Medium;
        }
        set
        {
            if (PreviewSize != value)
            {
                Preferences.Set(PreviewSizeKey, value.ToString());
                OnPreviewSizeChanged?.Invoke();
            }
        }
    }
}
