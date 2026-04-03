using BusinessLogic.Core.Features.Notes;
using BusinessLogic.Features.Notes;
using BusinessLogic.Features.NoteTemplates;
using Microsoft.Extensions.Logging;
using Noted.Services;

namespace Noted
{
    /// <summary>
    /// Entry point for the .NET MAUI Blazor application.
    /// Configures services, fonts, and application-wide settings.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI application.
        /// </summary>
        /// <returns>The configured <see cref="MauiApp"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// <b>Service Registration:</b>
        /// <list type="bullet">
        ///     <item><see cref="StorageService"/> - Manages note storage location preferences</item>
        ///     <item><see cref="FilterService"/> - Handles note filtering and sorting preferences</item>
        ///     <item><see cref="NoteManagement"/> - Core business logic for note CRUD operations</item>
        ///     <item><see cref="ThemeService"/> - Manages application theme (Catppuccin themes)</item>
        ///     <item><see cref="INotificationService"/> - Platform-specific notification handling</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Adding New Services:</b>
        /// Register new services here using the appropriate lifetime (Singleton, Scoped, Transient).
        /// Most app-wide services should be Singletons in MAUI applications.
        /// </para>
        /// </remarks>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add Blazor WebView support for hybrid app
            builder.Services.AddMauiBlazorWebView();
            
            // Register application services
            // StorageService must be registered first as NoteManagement depends on it
            builder.Services.AddSingleton<StorageService>();
            builder.Services.AddSingleton<FilterService>();
            builder.Services.AddSingleton<SidebarService>();
            builder.Services.AddSingleton<INoteManagement>(sp =>
            {
                var storageService = sp.GetRequiredService<StorageService>();
                return new NoteManagement(storageService.CurrentStorageLocation);
            });

            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<INoteTemplateManagement>(_ =>
                new NoteTemplateManagement(Path.Combine(FileSystem.AppDataDirectory, "TemplateLibrary")));

            // Content rendering services for Markdown and Rich Text
            builder.Services.AddSingleton<MarkdownService>();
            builder.Services.AddSingleton<RichTextService>();

#if DEBUG
            // Enable developer tools and debug logging in development builds
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Register services for ServiceLocator (for use in platform code)
            var sp = app.Services;
            ServiceLocator.Register(sp.GetRequiredService<INoteManagement>());
            ServiceLocator.Register(sp.GetRequiredService<INotificationService>());
            ServiceLocator.Register(sp.GetRequiredService<StorageService>());

            return app;
        }
    }
}
