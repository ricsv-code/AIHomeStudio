using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface ITTSService
    {
        Task SpeakAsync(string text);
        Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null);
        Task<List<string>?> GetAvailableModelsAsync();
    }

}
