# Noted - MAUI Blazor Hybrid App

A cross-platform note-taking application built with .NET MAUI Blazor Hybrid. Supports Android, iOS, macOS, and Windows.

## Project Structure

```
Noted/
??? Components/
?   ??? Layout/                      # Layout components
?   ?   ??? MainLayout.razor         # Root layout with sidebar
?   ?   ??? MainLayout.razor.css     # Layout styles
?   ?   ??? NavMenu.razor            # Navigation menu
?   ?   ??? NavMenu.razor.css        # Navigation styles
?   ??? Modals/                      # Modal dialog components
?   ?   ??? NoteEditModal.razor      # Create/edit note modal
?   ?   ??? NoteEditModal.razor.css  # Modal styles
?   ?   ??? DeleteConfirmationModal.razor
?   ?   ??? DeleteConfirmationModal.razor.css
?   ??? Pages/                       # Routable page components
?   ?   ??? Notes.razor              # Main notes list page
?   ?   ??? Notes.razor.css          # Notes page styles
?   ?   ??? Settings.razor           # App settings page
?   ?   ??? Settings.razor.css       # Settings page styles
?   ??? _Imports.razor               # Global using statements
?   ??? Routes.razor                 # Blazor router configuration
??? Services/                        # Application services
?   ??? StorageService.cs            # Note storage location management
?   ??? FilterService.cs             # Filtering and sorting preferences
?   ??? ThemeService.cs              # Theme management
?   ??? INotificationService.cs      # Notification interface
?   ??? NotificationService.cs       # Cross-platform notifications
??? Platforms/                       # Platform-specific code
?   ??? Android/
?   ??? iOS/
?   ??? MacCatalyst/
?   ??? Windows/
??? Resources/                       # App resources (icons, fonts, etc.)
??? wwwroot/                         # Static web assets (CSS, JS)
??? App.xaml                         # MAUI app resources
??? App.xaml.cs                      # App initialization
??? MainPage.xaml                    # Blazor WebView host page
??? MainPage.xaml.cs                 # Host page code-behind
??? MauiProgram.cs                   # App entry point and DI setup
??? Noted.csproj                     # Project file
```

## Key Features

- **Note Types**: Idea, Reminder (with notifications), Task (with status tracking)
- **Organization**: Color tags, pinning, filtering, sorting, search
- **Themes**: Catppuccin color schemes (Mocha, Macchiato, Frappé, Latte)
- **Custom Storage**: Configure where notes are saved
- **Cross-Platform**: Android, iOS, macOS, Windows

## Services

### StorageService
Manages the file system location where notes are stored.
- Default: `{AppDataDirectory}/Notes`
- Custom paths can be set in Settings
- **Important**: Changing location requires app restart

### FilterService
Persists user filter preferences across sessions:
- Search query
- Note type filter
- Sort option

### ThemeService
Manages the application's visual theme using Catppuccin color schemes.
Themes are applied via CSS variables on the root element.

### NotificationService
Cross-platform notification implementation:
| Platform | Technology |
|----------|------------|
| Android | AlarmManager + BroadcastReceiver |
| iOS/macOS | UNUserNotificationCenter |
| Windows | Windows App SDK with timer |

**Limitation**: Windows notifications require the app to be running.

## Adding a New Page

1. Create a new `.razor` file in `Components/Pages/`:
   ```razor
   @page "/mypage"
   
   <h1>My Page</h1>
   
   @code {
       // Component logic
   }
   ```

2. Add navigation in `NavMenu.razor`:
   ```razor
   <div class="nav-item px-3">
       <NavLink class="nav-link" href="mypage">
           <span class="nav-text">My Page</span>
       </NavLink>
   </div>
   ```

## Adding a New Service

1. Create the service class in `Services/`:
   ```csharp
   public class MyService
   {
       public void DoSomething() { }
   }
   ```

2. Register in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddSingleton<MyService>();
   ```

3. Inject into components:
   ```razor
   @inject MyService MyService
   ```

## Adding a New Theme

1. Add to `ThemeService.cs`:
   ```csharp
   public static IReadOnlyList<ThemeOption> AvailableThemes { get; } =
   [
       // existing themes...
       new("mytheme", "My Theme", "Description of my theme")
   ];
   ```

2. Define CSS variables in `wwwroot/css/app.css`:
   ```css
   [data-theme="mytheme"] {
       --background: #...;
       --foreground: #...;
       /* ... other variables */
   }
   ```

## Styling Conventions

- Component-scoped CSS: Create `ComponentName.razor.css` alongside the component
- Global styles: Add to `wwwroot/css/app.css`
- Theme variables: Defined per-theme with `[data-theme="..."]` selectors
- CSS classes follow BEM-like naming: `note-card`, `note-card--pinned`

## Dependencies

- .NET 10.0
- Microsoft.Maui.Controls
- Microsoft.AspNetCore.Components.WebView.Maui
- BusinessLogic project (sibling project)

## Building and Running

```bash
# Restore dependencies
dotnet restore

# Build for Windows
dotnet build -f net10.0-windows10.0.19041.0

# Build for Android
dotnet build -f net10.0-android

# Run (Windows)
dotnet run -f net10.0-windows10.0.19041.0
```

## Debugging

Debug logging is enabled in `DEBUG` builds. Check the Output window in Visual Studio for detailed logs.

For Blazor-specific debugging, enable developer tools:
```csharp
#if DEBUG
builder.Services.AddBlazorWebViewDeveloperTools();
#endif