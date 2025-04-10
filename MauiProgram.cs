using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using AIHomeStudio.Services;
using AIHomeStudio.Utilities;
using Microsoft.Maui.Controls;

namespace AIHomeStudio
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



            builder.Services.AddSingleton<AppConfiguration>();

            builder.Services.AddSingleton(AudioManager.Current);

            builder.Services.AddSingleton<IAuthenticator>(provider =>
            {
                string apiKey = provider.GetRequiredService<AppConfiguration>().MauiStorageKey;
                return new ApiKeyAuthenticator(apiKey);
            });

            builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();

            builder.Services.AddSingleton<IFastAPIService, FastAPIService>();

            builder.Services.AddSingleton<ICloudService, CloudService>();

            builder.Services.AddSingleton<IAIService, AIService>();

            builder.Services.AddSingleton<ISTTService, STTService>();

            builder.Services.AddSingleton<ITTSService, TTSService>();

            builder.Services.AddSingleton<ServiceManager>();

            builder.Services.AddSingleton<AIHomeStudioCore>();

            builder.Services.AddSingleton<MainPage>(provider => new MainPage(
                provider.GetRequiredService<AIHomeStudioCore>()
                ));

            builder.Services.AddSingleton<SplashPage>();

            builder.Services.AddSingleton<App>(provider => new App(
                provider.GetRequiredService<AIHomeStudioCore>(), 
                provider.GetRequiredService<SplashPage>()                    
                ));



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}