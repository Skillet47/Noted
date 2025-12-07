using BusinessLogic.Core;
using Microsoft.Extensions.Logging;
using Noted.Services;

namespace Noted
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            
            builder.Services.AddSingleton(new NoteManagement(
                Path.Combine(FileSystem.AppDataDirectory, "Notes")));
            
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
