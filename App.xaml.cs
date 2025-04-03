using AIHomeStudio.Utilities;

namespace AIHomeStudio
{
    public partial class App : Application
    {
        private readonly AIHomeStudioCore _core;

        public App(AIHomeStudioCore core)
        {
            InitializeComponent();

#if DEBUG
            ConsoleHelper.ShowConsole();
#endif
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            _core = core;

            MainPage = new SplashPage(); 

            StartAppAsync();
        }

        private async void StartAppAsync()
        {
            await _core.InitializeAsync(8000);
            MainPage = new MainPage(_core); 
        }
    }
}
