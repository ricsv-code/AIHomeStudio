using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using AIHomeStudio.Utilities;
using AIHomeStudio.Models;

namespace AIHomeStudio.Services
{
    public abstract class APIServiceBase
    {

        public event EventHandler<ServiceEventArgs>? OnServiceEvent;

        public ServiceType ServiceType { get; }
        protected readonly HttpClient _httpClient = new();
        protected string _baseUrl = "http://localhost:8000";


        protected APIServiceBase(ServiceType type)
        {
            Logger.Log($"Starting {type.ToString()}", this, true);

            ServiceType = type;


        }


        protected void RaiseEvent(ServiceEventType eventType, string message)
        {
            OnServiceEvent?.Invoke(this, new ServiceEventArgs(eventType, $"[{ServiceType}] {message}"));
            Logger.Log($"[{ServiceType}] {message}", this, false);
        }


        protected async Task<T?> SendRequestAsync<T>(
            HttpMethod method, 
            string endpoint, 
            object? payload = null
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

                var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}")
                {
                    Content = content
                };

                RaiseEvent(ServiceEventType.RequestSent, endpoint);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await JsonErrorHandler.GetErrorMessageFromResponse(response);
                    RaiseEvent(ServiceEventType.Error, $"Request failed: {errorMessage}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (HttpRequestException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Network error: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"JSON parsing error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Unexpected error: {ex.Message}");
                return null;
            }
        }


        protected async Task<string?> SendStreamingRequestAsync(
            HttpMethod method, 
            string endpoint, 
            object? payload = null, 
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

                var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}")
                {
                    Content = content
                };

                RaiseEvent(ServiceEventType.RequestSent, endpoint);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await JsonErrorHandler.GetErrorMessageFromResponse(response);
                    RaiseEvent(ServiceEventType.Error, $"Streaming request failed: {errorMessage}");
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
                        RaiseEvent(ServiceEventType.TokenReceived, token);
                    }
                }

                return responseBuilder.ToString();
            }
            catch (HttpRequestException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Network error during streaming: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"JSON parsing error during streaming: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Unexpected error during streaming: {ex.Message}");
                return null;
            }
        }


        protected async Task<List<string>?> GetModelsAsync(string endpoint)
        {
            StringListResponse? response = await SendRequestAsync<StringListResponse>(HttpMethod.Get, endpoint);
            return response?.Models;
        }


        protected async Task<bool> LoadModelStreamingAsync(
            string endpoint, 
            object payload, 
            Action<string>? onProgress
            )
        {
            try
            {
                string? json = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                RaiseEvent(ServiceEventType.RequestSent, endpoint);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await JsonErrorHandler.GetErrorMessageFromResponse(response);
                    RaiseEvent(ServiceEventType.Error, $"Model load failed: {errorMessage}");
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
                        RaiseEvent(ServiceEventType.LoadProgress, line);
                    }
                }

                RaiseEvent(ServiceEventType.Info, $"Model loaded.");
                return true;
            }
            catch (HttpRequestException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Network error loading model: {ex.Message}");
                return false;
            }
            catch (JsonException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"JSON parsing error loading model: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Unexpected error loading model: {ex.Message}");
                return false;
            }
        }
    }
}