using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ReadMe.Services;
using ReadMe.ViewModels;
namespace ReadMe
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<BookApiService>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();

#if DEBUG

            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
