using Microsoft.Maui.Storage;

namespace Noted.Services;

/// <summary>
/// Manages the application's color theme.
/// Supports Catppuccin color schemes (Mocha, Macchiato, Frappé, Latte).
/// </summary>
/// <remarks>
/// <para>
/// The theme is applied via the <c>data-theme</c> attribute on the root element
/// in <c>MainLayout.razor</c>. CSS variables for each theme are defined in the
/// application's stylesheet.
/// </para>
/// <para>
/// <b>Adding New Themes:</b>
/// 1. Add a new <see cref="ThemeOption"/> to the <see cref="AvailableThemes"/> list
/// 2. Define the corresponding CSS variables in the stylesheet under [data-theme="your-theme-id"]
/// </para>
/// </remarks>
public class ThemeService
{
    private const string ThemeKey = "AppTheme";
    private const string DefaultTheme = "mocha";
    private string _currentTheme;

    /// <summary>
    /// Event raised when the theme is changed.
    /// The MainLayout subscribes to this to update the UI.
    /// </summary>
    public event Action? OnThemeChanged;

    public ThemeService()
    {
        // Load persisted theme or use default
        _currentTheme = Preferences.Get(ThemeKey, DefaultTheme);
    }

    /// <summary>
    /// Gets or sets the current theme ID (e.g., "mocha", "latte").
    /// </summary>
    public string CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                Preferences.Set(ThemeKey, value);
                OnThemeChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets the list of available themes for the user to choose from.
    /// </summary>
    public static IReadOnlyList<ThemeOption> AvailableThemes { get; } =
    [
        new("mocha", "Catppuccin Mocha", "Dark theme with warm, cozy colors"),
        new("macchiato", "Catppuccin Macchiato", "Dark theme with slightly warmer tones"),
        new("frappe", "Catppuccin Frappé", "Medium dark theme with muted colors"),
        new("latte", "Catppuccin Latte", "Light theme for bright environments")
    ];
}

/// <summary>
/// Represents a selectable theme option.
/// </summary>
/// <param name="Id">The unique identifier used in CSS (data-theme attribute).</param>
/// <param name="Name">The display name shown to users.</param>
/// <param name="Description">A brief description of the theme's appearance.</param>
public record ThemeOption(string Id, string Name, string Description);
