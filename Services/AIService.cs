using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using AIHomeStudio.Utilities;
using AIHomeStudio.Models;

namespace AIHomeStudio.Services
{
    public class AIService : ServiceBase
    {
        private string _baseUrl = "http://localhost:8000";

        public AIService(int port) : base(ServiceType.AI, port)
        {
            _baseUrl = $"http://localhost:{port}";
        }

        public async Task AskAIStreamedAsync(
            string prompt = "",
            string systemPrompt = "",
            float temperature = 0.6f,
            float topP = 0.6f,
            int maxTokens = 100,
            Action<string>? onTokenReceived = null)
        {
            var payload = new
            {
                prompt,
                system_prompt = systemPrompt,
                temperature,
                top_p = topP,
                max_new_tokens = maxTokens
            };

            await SendStreamingRequestAsync(HttpMethod.Post, "/ai/generate_stream", payload, onTokenReceived);
        }

        public async Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null)
        {
            string modelPath = Path.Combine(AppContext.BaseDirectory, "Python", "ai_models", modelName);
            var payload = new { path = modelPath };
            return await LoadModelStreamingAsync("/ai/load", payload, onProgress);
        }

        public async Task<bool> CheckModelStatusAsync()
        {
            var result = await SendRequestAsync<dynamic>(HttpMethod.Get, "/ai/status");
            return result?.loaded == true;
        }

        public async Task<List<string>?> GetAvailableModelsAsync()
        {
            return await GetModelsAsync("/ai/models");
        }
    }
}