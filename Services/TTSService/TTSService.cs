using Newtonsoft.Json;
using Plugin.Maui.Audio;
using AIHomeStudio.Models;
using AIHomeStudio.Utilities;

namespace AIHomeStudio.Services
{
    public class TTSService : APIServiceBase, ITTSService
    {
        private readonly IAudioPlayerService _audioPlayer;
        private string _outputFilePath => Path.Combine(FileSystem.CacheDirectory, "output.wav");

        public TTSService(IAudioPlayerService audioPlayer) : base(ServiceType.TTS)
        {

            Logger.Log("Initializing..", this, true);
            _audioPlayer = audioPlayer;           

        }

        public async Task<List<string>?> GetAvailableModelsAsync()
        {
            return await GetModelsAsync("/tts/models");
        }

        public async Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null)
        {
            Logger.Log("Loading TTS models..", this, true);
            string modelPath = Path.Combine(AppContext.BaseDirectory, "Python", "tts_models", modelName);
            var payload = new { path = modelPath };
            return await LoadModelStreamingAsync("/tts/load", payload, onProgress);
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var payload = new { text };
            await SendRequestAsync<byte[]>(HttpMethod.Post, "/tts/speak", payload, async (audioBytes) =>
            {
                await File.WriteAllBytesAsync(_outputFilePath, audioBytes);
                await PlayAudioAsync(_outputFilePath);
            });
        }

        private async Task PlayAudioAsync(string path)
        {
            try
            {
                await _audioPlayer.PlayAudioAsync(path);
                RaiseEvent(ServiceEventType.Info, "Talking..");
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Error playing audio file: {ex.Message}");
            }
        }


        protected async Task<T?> SendRequestAsync<T>(
            HttpMethod method, 
            string endpoint, 
            object? payload = null, 
            Func<T, Task>? processResult = null
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
                    content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
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

                if (typeof(T) == typeof(byte[]))
                {
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    if (processResult != null)
                    {
                        await processResult((T)(object)responseBytes);
                    }
                    return null;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(responseContent);
                }
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
    }
}