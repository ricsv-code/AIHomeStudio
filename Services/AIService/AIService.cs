using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using AIHomeStudio.Utilities;
using AIHomeStudio.Models;

namespace AIHomeStudio.Services
{
    public class AIService : APIServiceBase, IAIService
    {

        #region Constructor
        public AIService() : base(ServiceType.AI)
        {
            Logger.Log("Initializing..", this, true);

        }
        #endregion


        #region Methods

        public async Task AskAIStreamedAsync(
            string prompt = "",
            string systemPrompt = "",
            float temperature = 0.6f,
            float topP = 0.6f,
            int maxTokens = 100,
            Action<string>? onTokenReceived = null,
            string? baseUrlOverride = null,
            bool useAuth = false
            )
        {
            var payload = new
            {
                prompt,
                system_prompt = systemPrompt,
                temperature,
                top_p = topP,
                max_new_tokens = maxTokens
            };

            await SendStreamingRequestAsync(HttpMethod.Post, "/ai/generate_stream", payload, onTokenReceived, baseUrlOverride, useAuth);
        }

        public async Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null, string baseUrlOverride = null, bool useAuth = false)
        {
            string modelPath = Path.Combine(AppContext.BaseDirectory, "Python", "ai_models", modelName);
            var payload = new { path = modelPath };
            return await LoadModelStreamingAsync("/ai/load", payload, onProgress, baseUrlOverride);
        }

        public async Task<bool> CheckModelStatusAsync(string? baseUrlOverride = null, bool useAuth = false)
        {
            var result = await SendRequestAsync<dynamic>(HttpMethod.Get, "/ai/status", null, baseUrlOverride, useAuth);
            return result?.loaded == true;
        }

        public async Task<List<string>?> GetAvailableModelsAsync(string baseUrlOverride = null, bool useAuth = false)
        {
            Logger.Log("Loading AI models..", this, true);
            return await GetModelsAsync("/ai/models", baseUrlOverride);
        }

        #endregion
    }
}