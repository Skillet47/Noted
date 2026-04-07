using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WRowDefinition = Microsoft.UI.Xaml.Controls.RowDefinition;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Noted.Platforms.Windows;

internal static class WindowsTitleBarThemeManager
{
    private const string ThemeKey = "AppTheme";
    private const string DefaultTheme = "mocha";

    private static WGrid? _titleBarRoot;
    private static WTextBlock? _titleBarText;
    private static WColumnDefinition? _leftInsetColumn;
    private static WColumnDefinition? _rightInsetColumn;
    private static AppWindow? _appWindow;
    private static WButton? _minimizeButton;
    private static WButton? _maximizeButton;
    private static WButton? _closeButton;
    private static bool _isMaximized = false;

    public static void ApplyCurrentTheme()
    {
        Apply(Preferences.Get(ThemeKey, DefaultTheme));
    }

    public static void Apply(string themeId)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        EnsureCustomTitleBar(nativeWindow);

        var palette = GetPalette(themeId);

        _titleBarRoot?.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Background);
        _titleBarText?.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Foreground);

        UpdateButtonColors(palette);
    }

    private static void EnsureCustomTitleBar(Microsoft.UI.Xaml.Window nativeWindow)
    {
        if (_titleBarRoot is not null)
        {
            return;
        }

        _appWindow = GetAppWindow(nativeWindow);

		if (nativeWindow.Content is not Microsoft.UI.Xaml.FrameworkElement existingContent)
		{
			return;
		}

		var hostGrid = new WGrid();
        hostGrid.RowDefinitions.Add(new WRowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        hostGrid.RowDefinitions.Add(new WRowDefinition { Height = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });

        _leftInsetColumn = new WColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(0) };
        _rightInsetColumn = new WColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(0) };

        _titleBarRoot = new WGrid
        {
            Height = 36,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
        };

        _titleBarRoot.ColumnDefinitions.Add(_leftInsetColumn);
        _titleBarRoot.ColumnDefinitions.Add(new WColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });
        _titleBarRoot.ColumnDefinitions.Add(new WColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(130) });
        _titleBarRoot.ColumnDefinitions.Add(_rightInsetColumn);

        CreateWindowControlButtons();
        var buttonStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
            Spacing = 0
        };
        buttonStack.Children.Add(_minimizeButton);
        buttonStack.Children.Add(_maximizeButton);
        buttonStack.Children.Add(_closeButton);
        _titleBarRoot.Children.Add(buttonStack);
        Microsoft.UI.Xaml.Controls.Grid.SetColumn(buttonStack, 2);

        hostGrid.Children.Add(_titleBarRoot);
        Microsoft.UI.Xaml.Controls.Grid.SetRow(_titleBarRoot, 0);

        hostGrid.Children.Add(existingContent);
        Microsoft.UI.Xaml.Controls.Grid.SetRow(existingContent, 1);

        nativeWindow.Content = hostGrid;

        nativeWindow.ExtendsContentIntoTitleBar = true;

		nativeWindow.SetTitleBar(_titleBarRoot);
		_appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        UpdateInsets();
        _appWindow.Changed += (_, _) => UpdateInsets();
    }

    private static void UpdateInsets()
    {
        if (_appWindow is null || _leftInsetColumn is null || _rightInsetColumn is null)
        {
            return;
        }

        _leftInsetColumn.Width = new Microsoft.UI.Xaml.GridLength(_appWindow.TitleBar.LeftInset);
        _rightInsetColumn.Width = new Microsoft.UI.Xaml.GridLength(_appWindow.TitleBar.RightInset);
    }

    private static AppWindow GetAppWindow(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private static TitleBarPalette GetPalette(string themeId)
    {
        return themeId switch
        {
            // Using --ctp-crust (ribbon/collapsed sidebar color)
            "macchiato" => new TitleBarPalette(Parse("#181926"), Parse("#CAD3F5"), Parse("#363A4F"), Parse("#A5ADCB"), Parse("#363A4F"), Parse("#494D64")),
            "frappe" => new TitleBarPalette(Parse("#232634"), Parse("#C6D0F5"), Parse("#414559"), Parse("#A5ADCE"), Parse("#414559"), Parse("#51576D")),
            "latte" => new TitleBarPalette(Parse("#DCE0E8"), Parse("#4C4F69"), Parse("#CCD0DA"), Parse("#6C6F85"), Parse("#CCD0DA"), Parse("#BCC0CC")),
            "dark" => new TitleBarPalette(Parse("#0D1017"), Parse("#ECEFF4"), Parse("#262B35"), Parse("#AEB7C6"), Parse("#262B35"), Parse("#343A46")),
            "white" => new TitleBarPalette(Parse("#EEF1F5"), Parse("#1F2430"), Parse("#ECEFF3"), Parse("#565E70"), Parse("#ECEFF3"), Parse("#D8DCE4")),
            "paper" => new TitleBarPalette(Parse("#E9DFD2"), Parse("#3F3429"), Parse("#F0E8DE"), Parse("#73665A"), Parse("#F0E8DE"), Parse("#DDD4CA")),
            _ => new TitleBarPalette(Parse("#11111B"), Parse("#CDD6F4"), Parse("#313244"), Parse("#A6ADC8"), Parse("#313244"), Parse("#45475A"))
        };
    }

    private static global::Windows.UI.Color Parse(string hex)
    {
        var value = hex.TrimStart('#');
        var hasAlpha = value.Length == 8;

        byte a = 255;
        var start = 0;
        if (hasAlpha)
        {
            a = Convert.ToByte(value[..2], 16);
            start = 2;
        }

        var r = Convert.ToByte(value.Substring(start, 2), 16);
        var g = Convert.ToByte(value.Substring(start + 2, 2), 16);
        var b = Convert.ToByte(value.Substring(start + 4, 2), 16);

        return global::Windows.UI.Color.FromArgb(a, r, g, b);
    }

    private static void CreateWindowControlButtons()
    {
        const double buttonWidth = 46;
        const double buttonHeight = 36;
        
        var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        var nativeWindow = (mauiWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window);

        _minimizeButton = new WButton
        {
            Width = buttonWidth,
            Height = buttonHeight,
            Content = "─",
            FontSize = 10,
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(0),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0)
        };
        _minimizeButton.Click += (_, _) => 
        {
            if (_appWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        };

        _maximizeButton = new WButton
        {
            Width = buttonWidth,
            Height = buttonHeight,
            Content = "□",
            FontSize = 10,
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(0),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0)
        };
        _maximizeButton.Click += (_, _) => ToggleMaximize();

        _closeButton = new WButton
        {
            Width = buttonWidth,
            Height = buttonHeight,
            Content = "✕",
            FontSize = 10,
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(0),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0)
        };
        _closeButton.Click += (_, _) => nativeWindow?.Close();
    }

    private static void ToggleMaximize()
    {
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            if (_isMaximized)
            {
                presenter.Restore();
                _isMaximized = false;
            }
            else
            {
                presenter.Maximize();
                _isMaximized = true;
            }
        }
    }

    private static void UpdateButtonColors(TitleBarPalette palette)
    {
        if (_minimizeButton is not null)
        {
            _minimizeButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Foreground);
            _minimizeButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Background);
        }

        if (_maximizeButton is not null)
        {
            _maximizeButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Foreground);
            _maximizeButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Background);
        }

        if (_closeButton is not null)
        {
            _closeButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Foreground);
            _closeButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(palette.Background);
        }
    }

    private readonly record struct TitleBarPalette(
        global::Windows.UI.Color Background,
        global::Windows.UI.Color Foreground,
        global::Windows.UI.Color InactiveBackground,
        global::Windows.UI.Color InactiveForeground,
        global::Windows.UI.Color HoverBackground,
        global::Windows.UI.Color PressedBackground);
}
