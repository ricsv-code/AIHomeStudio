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
            Logger.DebugLog("Registered AppConfiguration as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton(AudioManager.Current);
            Logger.DebugLog("Registered AudioManager.Current as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<IAuthenticator>(provider =>
            {
                string apiKey = provider.GetRequiredService<AppConfiguration>().MauiStorageKey;
                return new ApiKeyAuthenticator(apiKey);
            });
            Logger.DebugLog("Registered IAuthenticator, ApiKeyAuthenticator as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
            Logger.DebugLog("Registered IAudioPlayerService, AudioPlayerService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<IFastAPIService, FastAPIService>();
            Logger.DebugLog("Registered IFastAPIService, FastAPIService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<ICloudService, CloudService>();
            Logger.DebugLog("Registered ICloudService, CloudService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<ILocalAIService, LocalAIService>();
            Logger.DebugLog("Registered ILocalAIService, LocalAIService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<ISTTService, STTService>();
            Logger.DebugLog("Registered ISTTService, STTService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<ITTSService, TTSService>();
            Logger.DebugLog("Registered ITTSService, TTSService as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<ServiceManager>();
            Logger.DebugLog("Registered ServiceManager as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<AIHomeStudioCore>();
            Logger.DebugLog("Registered AIHomeStudioCore as Singleton", typeof(MauiProgram), false);

            builder.Services.AddSingleton<MainPage>(provider => new MainPage(
                provider.GetRequiredService<AIHomeStudioCore>()
                ));
            Logger.DebugLog("Registered MainPage as Singleton", typeof(MauiProgram), false);


            builder.Services.AddSingleton<SplashPage>();
            Logger.DebugLog("Registered SplashPage as Singleton", typeof(MauiProgram), false);


            builder.Services.AddSingleton<App>(provider => new App(
                provider.GetRequiredService<AIHomeStudioCore>(), 
                provider.GetRequiredService<SplashPage>()                    
                ));
            Logger.DebugLog("Registered App as Singleton", typeof(MauiProgram), false);



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}