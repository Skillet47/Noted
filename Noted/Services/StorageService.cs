namespace Noted.Services;

public class StorageService
{
    private const string StorageLocationKey = "NotesStorageLocation";
    private readonly string _defaultLocation;

    public event Action? OnStorageLocationChanged;

    public StorageService()
    {
        _defaultLocation = Path.Combine(FileSystem.AppDataDirectory, "Notes");
    }

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

    public string DefaultStorageLocation => _defaultLocation;

    public bool IsDefaultLocation => CurrentStorageLocation == _defaultLocation;

    public void ResetToDefault()
    {
        CurrentStorageLocation = _defaultLocation;
    }

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
