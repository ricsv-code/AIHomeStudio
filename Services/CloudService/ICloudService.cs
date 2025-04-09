using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Services
{
    public interface ICloudService
    {












        /// <summary>
        /// Attempts to retrieve a saved Cloud API key from MAUI SecureStorage
        /// </summary>
        /// <returns>The API key</returns>
        Task<string> GetApiKeyAsync();


        /// <summary>
        /// Attempts to save the Cloud key to MAUI SecureStorage
        /// </summary>
        /// <param name="apiKey">Retreived cloud key</param>
        /// <returns></returns>
        Task SaveApiKeyAsync(string apiKey);
    }
}
