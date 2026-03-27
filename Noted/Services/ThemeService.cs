namespace Noted.Services;

/// <summary>
/// Manages the application's color theme.
/// Supports Catppuccin color schemes (Mocha, Macchiato, Frapp�, Latte).
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
    private const string UiScaleKey = "AppUiScale";
    private const string DefaultTheme = "mocha";
    private const double DefaultUiScale = 1.0;
    private const double MinUiScale = 0.85;
    private const double MaxUiScale = 1.25;
    private string _currentTheme = Preferences.Get(ThemeKey, DefaultTheme);
    private double _uiScale = NormalizeScale(Preferences.Get(UiScaleKey, DefaultUiScale));

    /// <summary>
    /// Event raised when appearance preferences are changed.
    /// The MainLayout subscribes to this to update the UI.
    /// </summary>
    public event Action? OnThemeChanged;

    // Load persisted theme or use default

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
    /// Gets or sets the global UI scale multiplier for the application.
    /// </summary>
    public double UiScale
    {
        get => _uiScale;
        set
        {
            var normalized = NormalizeScale(value);

            if (Math.Abs(_uiScale - normalized) > 0.001)
            {
                _uiScale = normalized;
                Preferences.Set(UiScaleKey, normalized);
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
        new("frappe", "Catppuccin Frapp�", "Medium dark theme with muted colors"),
        new("latte", "Catppuccin Latte", "Light theme for bright environments")
    ];

    /// <summary>
    /// Gets the list of available UI scaling options.
    /// </summary>
    public static IReadOnlyList<UiScaleOption> AvailableUiScales { get; } =
    [
        new("compact", "Compact", "90% size for dense layouts", 0.90),
        new("default", "Default", "100% standard sizing", 1.00),
        new("comfortable", "Comfortable", "110% for improved readability", 1.10),
        new("large", "Large", "125% for maximum visibility", 1.25)
    ];

    private static double NormalizeScale(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return DefaultUiScale;
        }

        var clamped = Math.Clamp(value, MinUiScale, MaxUiScale);
        return Math.Round(clamped, 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Represents a selectable theme option.
/// </summary>
/// <param name="Id">The unique identifier used in CSS (data-theme attribute).</param>
/// <param name="Name">The display name shown to users.</param>
/// <param name="Description">A brief description of the theme's appearance.</param>
public record ThemeOption(string Id, string Name, string Description);

/// <summary>
/// Represents a selectable UI scale option.
/// </summary>
/// <param name="Id">The unique identifier for the option.</param>
/// <param name="Name">The display name shown to users.</param>
/// <param name="Description">A brief description of the scaling profile.</param>
/// <param name="Scale">The scale multiplier (for example, 1.10 = 110%).</param>
public record UiScaleOption(string Id, string Name, string Description, double Scale);
