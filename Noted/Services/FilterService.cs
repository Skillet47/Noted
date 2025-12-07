namespace Noted.Services;

public enum SortOption
{
    ModifiedNewest,
    ModifiedOldest,
    CreatedNewest,
    CreatedOldest,
    TitleAZ,
    TitleZA
}

public class FilterService
{
    private const string FilterTypeKey = "NotesFilterType";
    private const string SortOptionKey = "NotesSortOption";
    private const string SearchQueryKey = "NotesSearchQuery";

    public event Action? OnFilterChanged;

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

    public void ClearFilters()
    {
        Preferences.Remove(FilterTypeKey);
        Preferences.Remove(SortOptionKey);
        Preferences.Remove(SearchQueryKey);
        OnFilterChanged?.Invoke();
    }

    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(FilterType) ||
        !string.IsNullOrEmpty(SearchQuery) ||
        SortOption != SortOption.ModifiedNewest;

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
