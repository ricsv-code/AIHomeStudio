using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using AIHomeStudio.Utilities;
using AIHomeStudio.Models;

namespace AIHomeStudio.Services
{
    public static class ServiceHelper
    {
        public static async Task<T?> SendRequestAsync<T>(
            string baseUrl, 
            string endpoint, 
            ServiceType serviceType, 
            EventHandler<ServiceEventArgs>? onServiceEvent, 
            HttpMethod method, object? payload = null
            )
            where T : class
        {
            try
            {
                string? json = null;
                StringContent? content = null;
                if (payload != null)
                {
                    json = JsonConvert.SerializeObject(payload);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var request = new HttpRequestMessage(method, $"{baseUrl}{endpoint}")
                {
                    Content = content
                };

                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.RequestSent, endpoint));

                using var response = await new HttpClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ErrorHandler.GetErrorMessageFromResponse(response);
                    onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Request failed: {errorMessage}"));
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (HttpRequestException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Network error: {ex.Message}"));
                return null;
            }
            catch (JsonException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] JSON parsing error: {ex.Message}"));
                return null;
            }
            catch (Exception ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Unexpected error: {ex.Message}"));
                return null;
            }
        }

        public static async Task<string?> SendStreamingRequestAsync(
            string baseUrl, 
            string endpoint, 
            ServiceType serviceType, 
            EventHandler<ServiceEventArgs>? onServiceEvent, 
            HttpMethod method, object? payload = null, 
            Action<string>? onTokenReceived = null
            )
        {
            try
            {
                string? json = null;
                StringContent? content = null;
                if (payload != null)
                {
                    json = JsonConvert.SerializeObject(payload);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var request = new HttpRequestMessage(method, $"{baseUrl}{endpoint}")
                {
                    Content = content
                };

                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.RequestSent, endpoint));

                using var response = await new HttpClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ErrorHandler.GetErrorMessageFromResponse(response);
                    onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Streaming request failed: {errorMessage}"));
                    return null;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                char[] buffer = new char[1];
                StringBuilder responseBuilder = new();

                while (!reader.EndOfStream)
                {
                    int read = await reader.ReadAsync(buffer, 0, 1);
                    if (read > 0)
                    {
                        string token = buffer[0].ToString();
                        responseBuilder.Append(token);
                        onTokenReceived?.Invoke(token); 
                        onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.TokenReceived, token));
                    }
                }

                return responseBuilder.ToString();
            }
            catch (HttpRequestException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Network error during streaming: {ex.Message}"));
                return null;
            }
            catch (JsonException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] JSON parsing error during streaming: {ex.Message}"));
                return null;
            }
            catch (Exception ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Unexpected error during streaming: {ex.Message}"));
                return null;
            }
        }

        public static async Task<List<string>?> GetModelsAsync(
            string baseUrl, 
            string endpoint, 
            ServiceType serviceType, 
            EventHandler<ServiceEventArgs>? onServiceEvent)
        {
            var result = await SendRequestAsync<StringListResponse>(baseUrl, endpoint, serviceType, onServiceEvent, HttpMethod.Get);
            return result?.Models;
        }

        public static async Task<bool> LoadModelStreamingAsync(
            string baseUrl, 
            string endpoint, 
            ServiceType serviceType, 
            EventHandler<ServiceEventArgs>? onServiceEvent, 
            object payload, Action<string>? onProgress
            )
        {
            try
            {
                string? json = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{endpoint}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.RequestSent, endpoint));

                using var response = await new HttpClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ErrorHandler.GetErrorMessageFromResponse(response);
                    onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Model load failed: {errorMessage}"));
                    return false;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        onProgress?.Invoke(line);
                        onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.LoadProgress, line));
                    }
                }

                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Info, $"[{serviceType}] Model loaded."));
                return true;
            }
            catch (HttpRequestException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Network error loading model: {ex.Message}"));
                return false;
            }
            catch (JsonException ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] JSON parsing error loading model: {ex.Message}"));
                return false;
            }
            catch (Exception ex)
            {
                onServiceEvent?.Invoke(null, new ServiceEventArgs(ServiceEventType.Error, $"[{serviceType}] Unexpected error loading model: {ex.Message}"));
                return false;
            }
        }
    }
}