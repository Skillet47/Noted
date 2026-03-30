namespace Noted.Services;

/// <summary>
/// Service to coordinate sidebar state and notify components of changes.
/// Used to refresh the folder tree when notes or folders are created/deleted.
/// </summary>
public class SidebarService
{
    /// <summary>
    /// Event fired when the sidebar should refresh its folder list.
    /// </summary>
    public event Action? OnFoldersChanged;

    /// <summary>
    /// Event fired when notes should be refreshed.
    /// </summary>
    public event Action? OnNotesChanged;

    /// <summary>
    /// Currently selected folder in the sidebar.
    /// </summary>
    public string CurrentFolder { get; set; } = string.Empty;

    /// <summary>
    /// Currently selected note title.
    /// </summary>
    public string? SelectedNoteTitle { get; private set; }

    /// <summary>
    /// Folder of the currently selected note.
    /// </summary>
    public string? SelectedNoteFolder { get; private set; }

    /// <summary>
    /// True when the selected note was chosen from the pinned section.
    /// </summary>
    public bool SelectedInPinnedSection { get; private set; }

    /// <summary>
    /// Event fired when the selected folder changes.
    /// </summary>
    public event Action<string>? OnFolderSelected;

    /// <summary>
    /// Event fired when a note is selected.
    /// </summary>
    public event Action<string>? OnNoteSelected;

    /// <summary>
    /// Notifies subscribers that the folder list has changed.
    /// Call this after creating or deleting folders.
    /// </summary>
    public void NotifyFoldersChanged()
    {
        OnFoldersChanged?.Invoke();
    }

    /// <summary>
    /// Notifies subscribers that notes have changed.
    /// Call this after creating, editing, or deleting notes.
    /// </summary>
    public void NotifyNotesChanged()
    {
        OnNotesChanged?.Invoke();
    }

    /// <summary>
    /// Selects a folder and notifies subscribers.
    /// </summary>
    public void SelectFolder(string folder)
    {
        CurrentFolder = folder;
        SelectedNoteTitle = null;
        SelectedNoteFolder = null;
        SelectedInPinnedSection = false;
        OnFolderSelected?.Invoke(folder);
    }

    /// <summary>
    /// Selects a note and notifies subscribers.
    /// </summary>
    public void SelectNote(string noteTitle)
    {
        SelectedNoteFolder = CurrentFolder;
        SelectedInPinnedSection = false;
        SelectedNoteTitle = noteTitle;
        OnNoteSelected?.Invoke(noteTitle);
    }

    /// <summary>
    /// Selects a note and records where the note was selected from.
    /// </summary>
    public void SelectNote(string noteTitle, string folder, bool selectedInPinnedSection)
    {
        CurrentFolder = folder;
        SelectedNoteFolder = folder;
        SelectedInPinnedSection = selectedInPinnedSection;
        SelectedNoteTitle = noteTitle;
        OnNoteSelected?.Invoke(noteTitle);
    }
}
