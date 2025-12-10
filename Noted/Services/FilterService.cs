namespace Noted.Services;

/// <summary>
/// Defines the available sorting options for the notes list.
/// </summary>
public enum SortOption
{
    ModifiedNewest,
    ModifiedOldest,
    CreatedNewest,
    CreatedOldest,
    TitleAZ,
    TitleZA
}

/// <summary>
/// Manages filtering and sorting preferences for the notes list.
/// Persists user preferences across app sessions using MAUI Preferences.
/// </summary>
/// <remarks>
/// <para>
/// This service is used by the Notes page to maintain filter state.
/// Filter values are automatically persisted and restored on app restart.
/// </para>
/// <para>
/// <b>Adding New Filters:</b>
/// 1. Add a new property with getter/setter that uses Preferences
/// 2. Update <see cref="ClearFilters"/> to reset the new filter
/// 3. Update <see cref="HasActiveFilters"/> if the filter should indicate active state
/// 4. Update the Notes.razor component to use the new filter
/// </para>
/// </remarks>
public class FilterService
{
    // Preference keys for persisting filter values
    private const string FilterTypeKey = "NotesFilterType";
    private const string SortOptionKey = "NotesSortOption";
    private const string SearchQueryKey = "NotesSearchQuery";

    /// <summary>
    /// Event raised when any filter value changes.
    /// UI components should subscribe to update their display.
    /// </summary>
    public event Action? OnFilterChanged;

    /// <summary>
    /// Gets or sets the note type filter (e.g., "Reminder", "Task", "Idea").
    /// Empty string means no type filter is applied.
    /// </summary>
    public string FilterType
    {
        get => Preferences.Get(FilterTypeKey, string.Empty);
        set
        {
            if (FilterType != value)
            {
                Preferences.Set(FilterTypeKey, value);
                OnFilterChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current sort option for the notes list.
    /// </summary>
    public SortOption SortOption
    {
        get => Enum.TryParse<SortOption>(Preferences.Get(SortOptionKey, nameof(SortOption.ModifiedNewest)), out var result)
            ? result
            : SortOption.ModifiedNewest;
        set
        {
            if (SortOption != value)
            {
                Preferences.Set(SortOptionKey, value.ToString());
                OnFilterChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets or sets the search query for filtering notes by title/content.
    /// </summary>
    public string SearchQuery
    {
        get => Preferences.Get(SearchQueryKey, string.Empty);
        set
        {
            if (SearchQuery != value)
            {
                Preferences.Set(SearchQueryKey, value);
                OnFilterChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Resets all filters to their default values.
    /// </summary>
    public void ClearFilters()
    {
        Preferences.Remove(FilterTypeKey);
        Preferences.Remove(SortOptionKey);
        Preferences.Remove(SearchQueryKey);
        OnFilterChanged?.Invoke();
    }

    /// <summary>
    /// Gets whether any filter is currently active (non-default).
    /// Used to show/hide the "Clear Filters" button.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(FilterType) ||
        !string.IsNullOrEmpty(SearchQuery) ||
        SortOption != SortOption.ModifiedNewest;

    /// <summary>
    /// Gets a user-friendly display name for a sort option.
    /// </summary>
    /// <param name="option">The sort option to get the display name for.</param>
    /// <returns>A human-readable name for the sort option.</returns>
    public string GetSortOptionDisplayName(SortOption option) => option switch
    {
        SortOption.ModifiedNewest => "Modified (Newest)",
        SortOption.ModifiedOldest => "Modified (Oldest)",
        SortOption.CreatedNewest => "Created (Newest)",
        SortOption.CreatedOldest => "Created (Oldest)",
        SortOption.TitleAZ => "Title (A-Z)",
        SortOption.TitleZA => "Title (Z-A)",
        _ => option.ToString()
    };
}
