using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface ISTTService
    {
        Task StartListeningAsync();
        Task StopListeningAsync();
        Task CancelListeningAsync();
        Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null);
        Task<List<string>?> GetAvailableModelsAsync();
    }

}
