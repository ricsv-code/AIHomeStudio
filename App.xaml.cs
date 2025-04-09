using AIHomeStudio.Utilities;

namespace AIHomeStudio
{
    public partial class App : Application
    {
        private readonly AIHomeStudioCore _core;
        private readonly SplashPage _splashPage;

        public App(AIHomeStudioCore core, SplashPage splashPage)
        {
            Logger.Log($"App starting with params Core: {core} and SplashPage {splashPage}", this, true);

            InitializeComponent();

            Logger.Log("App-Component Initialized", this, true);
#if DEBUG
            ConsoleHelper.ShowConsole();
#endif
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            _core = core;
            _splashPage = splashPage;
            MainPage = _splashPage;


            Logger.Log("Starting App", this, true);
            StartApp();
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
                window.Destroying += App_Destroying;
                Logger.Log("Window was not null. Ready for destruction.", this, true);
            }

        }

        private async void StartApp()
        {

            await _core.InitializeAsync(8000);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new MainPage(_core);
            });

        }

        private async void App_Destroying(object? sender, EventArgs e)
        {
            MainPage = _splashPage;
            Logger.Log("Application is closing...", this, true);
            _core.Cleanup();
        }
    }
}