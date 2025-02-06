using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;

namespace LanguageCoachApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
            builder.Services.AddSingleton<MainPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
