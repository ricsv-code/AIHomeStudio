using AIHomeStudio.Services;
using Plugin.Maui.Audio;

namespace AIHomeStudio.Utilities
{
    public static class ServiceManager
    {

        #region Fields

        private static readonly object _lock = new();
        private static int _port;
        private static IAudioManager? _audioManager;

        private static AIService? _aiService;
        private static STTService? _sttService;
        private static TTSService? _ttsService;

        private static List<ServiceBase> _services;

        private static bool _initialized = false;

        #endregion

        #region Constructor

        public static void Initialize(IAudioManager audioManager, int port = 8000)
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return; 

                _audioManager = audioManager;
                _port = port;

                _services = new List<ServiceBase>();

                _aiService = new AIService(port);
                UIHooks.SplashLog("Starting AIService");
                Services.Add(_aiService);

                _sttService = new STTService(audioManager, port);
                UIHooks.SplashLog("Starting STTService");
                Services.Add(_sttService);


                _ttsService = new TTSService(audioManager, port);
                UIHooks.SplashLog("Starting TTSService");
                Services.Add(_ttsService);


                foreach (var service in _services)
                {
                    service.OnServiceEvent += OnServiceEvent;
                }


                _initialized = true;
            }
        }

        #endregion


        #region Events

        public static event EventHandler<ServiceEventArgs> OnServiceEvent;

        #endregion

        #region Properties

        public static int FastAPIPort => _port;



        public static List<ServiceBase> Services => _services ?? throw new InvalidOperationException("ServiceManager not initialized.");

        
        public static AIService AIService => _aiService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        public static STTService STTService => _sttService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        public static TTSService TTSService => _ttsService ?? throw new InvalidOperationException("ServiceManager not initialized.");

        public static IAudioManager AudioManager => _audioManager ?? throw new InvalidOperationException("ServiceManager not initialized.");


        #endregion
    }
}
