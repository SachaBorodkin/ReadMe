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

            var app = builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<LocalBooksService>();
            builder.Services.AddSingleton<EpubReaderService>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<ReaderViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ReaderPage>();

            var mauiApp = app.Build();
            Current = mauiApp;
            return mauiApp;
        }

        public static MauiApp Current { get; private set; }
    }
}
