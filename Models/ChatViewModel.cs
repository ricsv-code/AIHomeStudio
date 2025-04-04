using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;


namespace AIHomeStudio.Models 
{

    public class ChatViewModel : ViewModelBase
    {
        #region Fields

        private string _currentPrompt = "";
        private string _currentResponse = "";
        private string _errorMessage = "";
        private string _userPrefix = "GPT4 Correct User: ";
        private string _aiPrefix = "GPT4 Correct Assistant: ";
        private string _eot = "<|end_of_turn|>";

        #endregion

        #region Properties

        // chat
        public string CurrentPrompt
        {
            get => _currentPrompt;
            set => SetProperty(ref _currentPrompt, value);
        }


        public string CurrentResponse
        {
            get => _currentResponse;
            set => SetProperty(ref _currentResponse, value);
        }

        // error

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // prefix

        public string UserPrefix
        {
            get => _userPrefix;
            set => SetProperty(ref _userPrefix, value);
        }

        public string AIPrefix
        {
            get => _aiPrefix;
            set => SetProperty(ref _aiPrefix, value);
        }

        public string EndOfTurnToken
        {
            get => _eot;
            set => SetProperty(ref _eot, value);
        }

        // Chat memory
        public ObservableCollection<ChatMessage> Memory { get; set; } = new();

        #endregion

        #region Methods

        public void AppendMemory(string role, string message)
        {
            Memory.Add(new ChatMessage { Role = role, Text = message });
        }

        public void ClearMemory()
        {
            Memory.Clear();
        }

        public string GetFormattedConversation()
        {
            var sb = new StringBuilder();
            foreach (var msg in Memory)
            {
                if (msg.Role == "user")
                    sb.AppendLine($"{UserPrefix}{msg.Text}{EndOfTurnToken}\n");
                else if (msg.Role == "assistant")
                    sb.AppendLine($"{AIPrefix}{msg.Text}{EndOfTurnToken}\n");
            }

            return sb.ToString();
        }

        #endregion
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user"; 
        public string Text { get; set; } = "";
    }

}
