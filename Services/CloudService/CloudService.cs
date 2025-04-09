using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AIHomeStudio.Utilities;

namespace AIHomeStudio.Services
{
    public class CloudService : APIServiceBase, ICloudService
    {

        private AppConfiguration _config;
        private string _apiKeyStorageKey => _config.MauiStorageKey;

        public CloudService(IAuthenticator authenticator, AppConfiguration config) : base(ServiceType.Cloud)
        {
            Logger.Log("Initializing..", this, true);
            _config = config;
        }






        public async Task<string> GetApiKeyAsync()
        {
            try
            {
                string apiKey = await SecureStorage.GetAsync(_apiKeyStorageKey);
                return apiKey;
            }
            catch (Exception ex)
            {

                RaiseEvent(ServiceEventType.Error, $"Error getting API key: {ex.Message}");
                return null;
            }
        }


        public async Task SaveApiKeyAsync(string apiKey)
        {
            try
            {
                await SecureStorage.SetAsync(_apiKeyStorageKey, apiKey);
            }
            catch (Exception ex)
            {
                if (ex is PlatformNotSupportedException)
                    RaiseEvent(ServiceEventType.Error, $"Platform error: {ex.Message}");
                else
                {
                    RaiseEvent(ServiceEventType.Error, $"Error saving API key: {ex.Message}");
                }
            }
        }

    }
}
