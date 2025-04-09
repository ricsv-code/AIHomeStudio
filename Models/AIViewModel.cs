using AIHomeStudio.Utilities;

namespace AIHomeStudio.Models
{
    public class AIViewModel : ViewModelBase
    {

        private ServiceManager _serviceManager;
        public AIViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }
        // PARAMS

        private float _temperature = 0.7f;
        public float Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        private float _topP = 0.9f;
        public float TopP
        {
            get => _topP;
            set => SetProperty(ref _topP, value);
        }

        private int _maxTokens = 200;
        public int MaxTokens
        {
            get => _maxTokens;
            set => SetProperty(ref _maxTokens, value);
        }

        private string _systemPrompt = "Du är en hjälpsam AI.";
        public string SystemPrompt
        {
            get => _systemPrompt;
            set => SetProperty(ref _systemPrompt, value);
        }


        // LOADING

        private string _loadingText = "";
        public string LoadingText
        {
            get => _loadingText;
            set => SetProperty(ref _loadingText, value);
        }


        private string _infoText = "";
        public string InfoText
        {
            get => _infoText;
            set => SetProperty(ref _infoText, value);
        }




        // MODEL

        private List<string> _availableModels = new();
        public List<string> AvailableModels
        {
            get => _availableModels;
            set => SetProperty(ref _availableModels, value);
        }

        private string _chosenModel = "";
        public string ChosenModel
        {
            get => _chosenModel;
            set {
                if (SetProperty(ref _chosenModel, value))
                {
                    _ = _serviceManager.AIService.LoadModelAsync(value);
                }
            }
        }








        //LATER USE?


        private int _port = 8000;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private bool _isModelLoaded;
        public bool IsModelLoaded
        {
            get => _isModelLoaded;
            set => SetProperty(ref _isModelLoaded, value);
        }
    }
}
