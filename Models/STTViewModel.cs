using AIHomeStudio.Utilities;

namespace AIHomeStudio.Models
{
    public class STTViewModel : ViewModelBase
    {


        private string _currentSpeech = string.Empty;
        public string CurrentSpeech
        {
            get => _currentSpeech;
            set => SetProperty(ref _currentSpeech, value);
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
                    _ = ServiceManager.STTService.LoadModelAsync(value);
                }
            }
        }


        // LATER USE?

        private bool _isSTTReady;
        public bool IsSTTReady
        {
            get => _isSTTReady;
            set => SetProperty(ref _isSTTReady, value);
        }
    }
}
