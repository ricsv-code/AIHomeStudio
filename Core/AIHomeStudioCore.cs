using AIHomeStudio.Models;
using System.Windows.Input;
using System.Text;
using System.Timers;
using AIHomeStudio.Utilities;
using System.Diagnostics;
using Plugin.Maui.Audio;
using System.Runtime.ConstrainedExecution;
using AIHomeStudio.Services;

namespace AIHomeStudio
{
    public class AIHomeStudioCore
    {
        #region Fields
        private bool _isAwaitingResponse;
        private TTSBufferManager _ttsBuffer;


        private AppConfiguration _config;
        private ServiceManager _serviceManager;


        #endregion

        #region Commands

        public ICommand SendCommand { get; private set; }
        public ICommand StartListeningCommand { get; private set; }
        public ICommand StopListeningCommand { get; private set; }

        #endregion

        #region Constructor
        public AIHomeStudioCore(AppConfiguration config, ServiceManager serviceManager)
        {
            Logger.Log("Initializing..", this, true);

            SendCommand = new RelayCommand(Send);

            _config = config;
            _serviceManager = serviceManager;

            StartListeningCommand = new RelayCommand(async () => await StartListeningAsync());
            StopListeningCommand = new RelayCommand(async () => await StopListeningAsync());



        }
        #endregion

        #region Properties

        public STTViewModel STT { get; private set; }
        public TTSViewModel TTS { get; private set; }
        public ChatViewModel Chat { get; private set; } 
        public AIViewModel AI { get; private set; }


        #endregion



        #region Methods


        // init
        public async Task InitializeAsync(int port)
        {
            STT = new STTViewModel(_serviceManager);
            TTS = new TTSViewModel(_serviceManager);
            Chat = new ChatViewModel();
            AI = new AIViewModel(_serviceManager);


            await _serviceManager.FastAPIService.StartAsync(port);

            
            AI.AvailableModels = await _serviceManager.AIService.GetAvailableModelsAsync();

            STT.AvailableModels = await _serviceManager.STTService.GetAvailableModelsAsync();

            TTS.AvailableModels = await _serviceManager.TTSService.GetAvailableModelsAsync();


            Logger.Log("All local models loaded.", this, true);


            Chat.UserPrefix = "GPT4 Correct User: ";
            Chat.AIPrefix = "GPT4 Correct Assistant: ";
            Chat.EndOfTurnToken = "<|end_of_turn|>";

            _ttsBuffer = new TTSBufferManager();
            _ttsBuffer.OnBufferReady += TTSBufferManager_OnBufferReady;


            _serviceManager.OnServiceEvent -= ServiceManager_OnServiceEvent;
            _serviceManager.OnServiceEvent += ServiceManager_OnServiceEvent;


            AI.ChosenModel = AI.AvailableModels?.FirstOrDefault();
            STT.ChosenModel = STT.AvailableModels?.FirstOrDefault();
            TTS.ChosenModel = TTS.AvailableModels?.FirstOrDefault();

        }



        // Buttons



        private async void Send(object sender)
        {
            await _serviceManager.AIService.AskAIStreamedAsync(
                Chat.CurrentPrompt, 
                AI.SystemPrompt, 
                AI.Temperature,
                AI.TopP,
                AI.MaxTokens
                );

            _isAwaitingResponse = true;
        }
        private async Task StartListeningAsync()
        {
            await _serviceManager.STTService.StartListeningAsync();
        }

        private async Task StopListeningAsync()
        {
            await _serviceManager.STTService.StopListeningAsync();
        }










        // ServiceEvents
        private void ServiceManager_OnServiceEvent(object? sender, Services.ServiceEventArgs e)
        {
            if (sender is not APIServiceBase service)
                return;

            if (e.EventType == ServiceEventType.Error)
            {
                HandleError(e);
                return;
            }

            switch (service.ServiceType)
            {
                case ServiceType.AI:
                    HandleAIEvent(e);
                    break;

                case ServiceType.TTS:
                    HandleTTSEvent(e);
                    break;

                case ServiceType.STT:
                    HandleSTTEvent(e);
                    break;

                default:
                    break;
            }
        }


        // Handle error
        private void HandleError(ServiceEventArgs e)
        {
            Chat.ErrorMessage += $"{e.Message}\n";
        }


        // AI Event
        private void HandleAIEvent(ServiceEventArgs e)
        {
            switch (e.EventType)
            {
                case ServiceEventType.LoadProgress:
                    AI.LoadingText = e.Message;
                    break;
                case ServiceEventType.TokenReceived:

                    string token = e.Message;

                    if (token == "[END]")
                    {
                        Chat.AppendMemory(Chat.AIPrefix, Chat.CurrentResponse);
                        Chat.CurrentResponse = "";
                        _isAwaitingResponse = false;

                        _ttsBuffer.Reset();
                        return;
                    }

                    Chat.CurrentResponse += token;

                    if (TTS.IsTTSReady)
                    {
                        _ttsBuffer.PushToken(token);
                    }

                    break;
                case ServiceEventType.RequestSent:

                    AI.InfoText = e.Message;

                    break;

                case ServiceEventType.Info:

                    break;

                default:
                    break;
            }
        }


        // TTS Event
        private void HandleTTSEvent(ServiceEventArgs e)
        {
            switch (e.EventType)
            {
                case ServiceEventType.RequestSent:

                    TTS.InfoText = e.Message;

                    break;
                case ServiceEventType.LoadProgress:

                    TTS.LoadingText = e.Message;

                    break;
                case ServiceEventType.Info:

                    if (e.Message == "[TTS] Model loaded.")
                        TTS.IsTTSReady = true;


                    TTS.InfoText = e.Message;

                    break;

                default:
                    break;
            }
        }

        // STT Event
        private void HandleSTTEvent(ServiceEventArgs e)
        {
            switch (e.EventType)
            {
                case ServiceEventType.RequestSent:

                    STT.InfoText = e.Message;
                    break;
                case ServiceEventType.LoadProgress:

                    STT.LoadingText = e.Message;
                    break;
                case ServiceEventType.Info:

                    if (e.Message == "[STT] End of speech detected.")
                        Send(this);

                        STT.InfoText = e.Message;

                    break;
                case ServiceEventType.TokenReceived:

                    Chat.CurrentPrompt += e.Message;
                    STT.CurrentSpeech = e.Message;
                    break;
                default:
                    break;
            }
        }



        // TTS buffer
        private async void TTSBufferManager_OnBufferReady(object? sender, string e)
        {
            await _serviceManager.TTSService.SpeakAsync(e);
        }


        // clean
        public void Cleanup()
        {


        }



        #endregion
    }
}
