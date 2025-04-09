using AIHomeStudio.Utilities;

namespace AIHomeStudio.Models
{
    public class TTSViewModel : ViewModelBase
    {

        private ServiceManager _serviceManager;

        public TTSViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
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
            set
            {
                if (SetProperty(ref _chosenModel, value))
                {
                    _ = _serviceManager.TTSService.LoadModelAsync(value);
                }
            }
        }


        // LATER USE?

        private bool _isTTSReady;
        public bool IsTTSReady
        {
            get => _isTTSReady;
            set => SetProperty(ref _isTTSReady, value);
        }

    }
}
