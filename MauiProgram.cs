using Microsoft.Extensions.Logging;
using MauiApp1.Services;  

namespace MauiApp1
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

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<MarkdownService>();
            builder.Services.AddSingleton<SecurityService>();
            builder.Services.AddSingleton<ThemeState>();   
            builder.Services.AddSingleton<PdfExportService>(); 

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
