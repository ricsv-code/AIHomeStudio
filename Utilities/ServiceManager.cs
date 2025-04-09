using AIHomeStudio.Services;
using Plugin.Maui.Audio;

namespace AIHomeStudio.Utilities
{
    public class ServiceManager
    {

        #region Fields

        private readonly object _lock = new();


        private ILocalAIService? _aiService;
        private ISTTService? _sttService;
        private ITTSService? _ttsService;
        private ICloudService? _cloudService;

        private IFastAPIService? _fastAPIService;

        private List<object> _services;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ServiceManager(
            ILocalAIService aiService, 
            ISTTService sttService, 
            ITTSService ttsService,
            ICloudService cloudService,
            IFastAPIService fastAPIService            
            )
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                Logger.Log("Initializing..", this, true);
                _aiService = aiService;
                _sttService = sttService;
                _ttsService = ttsService;

                _cloudService = cloudService;

                _fastAPIService = fastAPIService;

                _services = new List<object>();


                Initialize();

                _initialized = true;
            }
        }

        #endregion


        #region Events

        public event EventHandler<ServiceEventArgs> OnServiceEvent;

        #endregion

        #region Properties


        public List<object> Services => _services ?? throw new InvalidOperationException("ServiceManager not initialized.");

        
        public ILocalAIService AIService => _aiService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        public ISTTService STTService => _sttService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        public ITTSService TTSService => _ttsService ?? throw new InvalidOperationException("ServiceManager not initialized.");


        public ICloudService CloudService => _cloudService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        public IFastAPIService FastAPIService => _fastAPIService ?? throw new InvalidOperationException("ServiceManager not initialized.");
        




        #endregion

        #region Methods
        private void Initialize()
        {
            Logger.Log("Initializing ServiceManager", this, true);

            Services.Add(_aiService);
            Services.Add(_sttService);            
            Services.Add(_ttsService);

            foreach (var service in _services)
            {
                if (service is APIServiceBase serv)
                    serv.OnServiceEvent += OnServiceEvent;
            }
        }


        #endregion
    }
}
