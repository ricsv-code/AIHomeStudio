using Plugin.Maui.Audio;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AIHomeStudio.Services
{
    public class STTService : ServiceBase
    {
        private static readonly HttpClient _httpClient = new();
        private readonly IAudioManager _audioManager;
        private IAudioRecorder? _recorder;
        private CancellationTokenSource? _cts;

        private string _baseUrl = "http://localhost:8000";

        public STTService(IAudioManager audioManager, int port) : base(ServiceType.STT)
        {
            _audioManager = audioManager;
            _baseUrl = $"http://localhost:{port}";
        }

        public bool IsRecording => _recorder?.IsRecording ?? false;

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/stt/models");
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<STTModelListResponse>(json);
                return result?.Models ?? new List<string>();
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[STT] Failed to fetch model list: " + ex.Message);
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

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/stt/load")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    RaiseEvent(ServiceEventType.Error, $"[STT] Failed to load model: {modelName}");
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
                RaiseEvent(ServiceEventType.Error, "[STT] Load model exception: " + ex.Message);
                return false;
            }
        }


        public async Task StartListeningAsync()
        {
            if (_recorder != null && _recorder.IsRecording)
            {
                await SafeStopAndDiscardAsync(_recorder);
                _recorder = null;
            }

            _recorder = _audioManager.CreateRecorder();
            _cts = new CancellationTokenSource();

            string tempFile = Path.Combine(FileSystem.CacheDirectory, $"live_{DateTime.Now.Ticks}.wav");

            await _recorder.StartAsync();
            RaiseEvent(ServiceEventType.Info, "[STT] Recording started.");

            await Task.Delay(500); 

            _ = Task.Run(async () =>
            {
                try
                {
                    while (_recorder != null && !_recorder.IsRecording)
                        await Task.Delay(100, _cts.Token);

                    RaiseEvent(ServiceEventType.Info, "[STT] Streaming audio to server.");

                    await TranscribeLiveAsync(tempFile, partial =>
                    {
                        RaiseEvent(ServiceEventType.TokenReceived, partial);
                    }, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    RaiseEvent(ServiceEventType.Info, "[STT] Streaming cancelled.");
                }
                catch (Exception ex)
                {
                    RaiseEvent(ServiceEventType.Error, "[STT] Streaming error: " + ex.Message);
                }
            }, _cts.Token);
        }

        public async Task StopListeningAsync()
        {
            if (_recorder == null)
                return;

            try
            {
                _cts?.Cancel();
                await _recorder.StopAsync();
                RaiseEvent(ServiceEventType.Info, "[STT] Recording stopped.");
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[STT] Error during stop: " + ex.Message);
            }
            finally
            {
                _recorder = null;
                _cts = null;
            }
        }

        public async Task CancelListeningAsync()
        {
            _cts?.Cancel();
            await SafeStopAndDiscardAsync(_recorder);
            _recorder = null;
            _cts = null;
        }

        private async Task SafeStopAndDiscardAsync(IAudioRecorder? recorder)
        {
            try
            {
                if (recorder != null && recorder.IsRecording)
                    await recorder.StopAsync();
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[STT] Failed to stop recorder: " + ex.Message);
            }
        }

        private async Task TranscribeLiveAsync(string wavFilePath, Action<string> onPartialReceived, CancellationToken cancellationToken)
        {
            using var content = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(wavFilePath, cancellationToken);
            var byteContent = new ByteArrayContent(fileBytes);
            byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            content.Add(byteContent, "file", Path.GetFileName(wavFilePath));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/stt/stream")
                {
                    Content = content
                };

                RaiseEvent(ServiceEventType.RequestSent, "[STT] Sending stream request...");

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    RaiseEvent(ServiceEventType.Error, "[STT] Stream API error.");
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        onPartialReceived(line);
                }
            }
            catch (TaskCanceledException)
            {
                RaiseEvent(ServiceEventType.Info, "[STT] Recording stream cancelled.");
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, "[STT] Streaming failed: " + ex.Message);
            }
        }

        private class STTModelListResponse
        {
            [JsonProperty("models")]
            public List<string>? Models { get; set; }
        }
    }
}
