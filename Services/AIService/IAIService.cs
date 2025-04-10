using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface IAIService
    {
        Task AskAIStreamedAsync(
            string prompt,
            string systemPrompt,
            float temperature,
            float topP,
            int maxTokens,
            Action<string>? onTokenReceived = null,
            string? baseUrlOverride = null,
            bool useAuth = false
            );

        Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null, string? baseUrlOverride = null, bool useAuth = false);
        Task<bool> CheckModelStatusAsync(string? baseUrlOverride = null, bool useAuth = false);
        Task<List<string>?> GetAvailableModelsAsync(string? baseUrlOverride = null, bool useAuth = false);
    }

}
