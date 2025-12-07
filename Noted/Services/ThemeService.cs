namespace Noted.Services;

public class ThemeService
{
    private const string DefaultTheme = "mocha";
    private string _currentTheme = DefaultTheme;

    public event Action? OnThemeChanged;

    public string CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnThemeChanged?.Invoke();
            }
        }
    }

    public static IReadOnlyList<ThemeOption> AvailableThemes { get; } =
    [
        new("mocha", "Catppuccin Mocha", "Dark theme with warm, cozy colors"),
        new("macchiato", "Catppuccin Macchiato", "Dark theme with slightly warmer tones"),
        new("frappe", "Catppuccin Frappé", "Medium dark theme with muted colors"),
        new("latte", "Catppuccin Latte", "Light theme for bright environments")
    ];
}

public record ThemeOption(string Id, string Name, string Description);
