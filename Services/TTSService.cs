using Newtonsoft.Json;
using Plugin.Maui.Audio;
using System.Net.Http.Headers;

namespace AIHomeStudio.Services
{
    public class TTSService : ServiceBase
    {
        private readonly IAudioManager _audioManager;
        private readonly HttpClient _httpClient = new();
        private string _baseUrl = "http://localhost:8000";
        private string _outputFilePath => Path.Combine(FileSystem.CacheDirectory, "output.wav");

        public TTSService(IAudioManager audioManager, int port) : base(ServiceType.TTS)
        {
            _audioManager = audioManager;
            _baseUrl = $"http://localhost:{port}";
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/tts/models");
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                RaiseEvent(ServiceEventType.RequestSent, "Fetching models..");

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<ModelListResponse>(json);
                return parsed?.Models ?? new List<string>();
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[TTS] Error fetching models: " + ex.Message);
                return new List<string>();
            }
        }

        public async Task<bool> LoadModelAsync(string modelName)
        {
            try
            {
                var content = new MultipartFormDataContent
                {
                    { new StringContent(modelName), "lang" }
                };

                var response = await _httpClient.PostAsync($"{_baseUrl}/tts/load", content);
                if (!response.IsSuccessStatusCode)
                {
                    RaiseEvent(ServiceEventType.Error, $"[TTS] Could not load model {modelName}");
                    return false;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        RaiseEvent(ServiceEventType.LoadProgress, line);
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[TTS] Load model failed: " + ex.Message);
                return false;
            }
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var payload = new { text };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/tts/speak", content);
                if (!response.IsSuccessStatusCode)
                {
                    RaiseEvent(ServiceEventType.Error, "[TTS] Speak API failed.");
                    return;
                }

                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(_outputFilePath, audioBytes);
                await PlayAudioAsync(_outputFilePath);
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[TTS] Playback failed: " + ex.Message);
            }
        }

        private async Task PlayAudioAsync(string path)
        {
            await using var stream = File.OpenRead(path);
            var player = _audioManager.CreatePlayer(stream);
            player.Play();
        }



        private class ModelListResponse
        {
            [JsonProperty("models")]
            public List<string>? Models { get; set; }
        }
    }
}
