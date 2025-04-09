using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface ILocalAIService
    {
        Task AskAIStreamedAsync(
            string prompt,
            string systemPrompt,
            float temperature,
            float topP,
            int maxTokens,
            Action<string>? onTokenReceived = null);

        Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null);
        Task<bool> CheckModelStatusAsync();
        Task<List<string>?> GetAvailableModelsAsync();
    }

}
