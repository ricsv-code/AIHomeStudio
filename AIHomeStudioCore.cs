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

        private readonly Initializer _initializer = new();




        #endregion

        #region Constructor
        public AIHomeStudioCore()
        {
            SendCommand = new RelayCommand(Send);




        }
        #endregion

        #region Properties

        public STTViewModel STT { get; private set; } = new();
        public TTSViewModel TTS { get; private set; } = new();
        public ChatViewModel Chat { get; private set; } = new();
        public AIViewModel AI { get; private set; } = new();

        public ICommand LoadAIModelCommand { get; private set; }
        public ICommand LoadSTTModelCommand { get; private set; }
        public ICommand LoadTTSModelCommand { get; private set; }
        public ICommand SendCommand { get; private set; }

        #endregion

        #region Methods


        private async void TTSBufferManager_OnBufferReady(object? sender, string e)
        {
            await ServiceManager.TTSService.SpeakAsync(e);
        }

        private void AIService_OnTokenReceived(object? sender, string token)
        {
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
        }

        private void STTService_OnSpeechRecognized(object? sender, string e)
        {
            if (_isAwaitingResponse)
                return;

            STT.CurrentSpeech = e;
            Chat.CurrentPrompt += e;

            Chat.AppendMemory("User", Chat.CurrentPrompt);

            _isAwaitingResponse = true;
            Chat.CurrentResponse = "";

            ServiceManager.AIService.AskAIStreamedAsync(Chat.CurrentPrompt);
            Chat.CurrentPrompt = "";
        }

        // AI


        private async void Send(object sender)
        {
            await ServiceManager.AIService.AskAIStreamedAsync(Chat.CurrentPrompt);
        }

        // STT


        public async Task StartListeningAsync()
        {
            await ServiceManager.STTService.StartListeningAsync();
        }

        public async Task StopListeningAsync()
        {
            await ServiceManager.STTService.StopListeningAsync();
        }

        // TTS





        // init
        public async Task InitializeAsync(int port)
        {

            await _initializer.InitializeAllAsync(AI, STT, TTS, AudioManager.Current, port);


            Chat.UserPrefix = "GPT4 Correct User: ";
            Chat.AIPrefix = "GPT4 Correct Assistant: ";
            Chat.EndOfTurnToken = "<|end_of_turn|>";


            _ttsBuffer = new TTSBufferManager();

            _ttsBuffer.OnBufferReady += TTSBufferManager_OnBufferReady;



            ServiceManager.OnServiceEvent -= ServiceManager_OnServiceEvent;
            ServiceManager.OnServiceEvent += ServiceManager_OnServiceEvent;


            





            AI.ChosenModel = AI.AvailableModels?.FirstOrDefault();
            STT.ChosenModel = STT.AvailableModels?.FirstOrDefault();
            TTS.ChosenModel = TTS.AvailableModels?.FirstOrDefault();
        }

        private void ServiceManager_OnServiceEvent(object? sender, Services.ServiceEventArgs e)
        {
            if (sender is not ServiceBase service)
                return;

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


        // AI Event
        private void HandleAIEvent(ServiceEventArgs e)
        {
            switch (e.EventType)
            {
                case ServiceEventType.LoadProgress:

                    AI.LoadingText = e.Message;

                    break;
                case ServiceEventType.TokenReceived:

                    Chat.CurrentResponse += e.Message;

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
                case ServiceEventType.LoadProgress:

                    AI.LoadingText = e.Message;

                    break;
                case ServiceEventType.TokenReceived:

                    

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
                case ServiceEventType.LoadProgress:

                    STT.LoadingText = e.Message;

                    break;
                case ServiceEventType.TokenReceived:

                    

                    break;

                default:
                    break;

            }
        }


        public void Cleanup()
        {
            _initializer.Cleanup();
        }



        #endregion
    }
}
