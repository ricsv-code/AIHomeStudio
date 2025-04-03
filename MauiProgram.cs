using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using AIHomeStudio.Services;

namespace AIHomeStudio
{
    public static class MauiProgram
    {
        private static AIHomeStudioCore? _core;

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

            builder.Services.AddSingleton(AudioManager.Current);

            builder.Services.AddSingleton<AIHomeStudioCore>();

            builder.Services.AddSingleton<MainPage>(provider =>
            {
                var core = provider.GetRequiredService<AIHomeStudioCore>();
                return new MainPage(core);
            });

            builder.Services.AddSingleton<App>();
                        

            AppDomain.CurrentDomain.ProcessExit += (_, _) => _core?.Cleanup();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
