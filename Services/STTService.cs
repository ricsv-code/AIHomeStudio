using Newtonsoft.Json;
using Plugin.Maui.Audio;
using System.Net.Http.Headers;
using AIHomeStudio.Utilities;
using AIHomeStudio.Models;
using System.Text;

namespace AIHomeStudio.Services
{
    public class STTService : ServiceBase
    {
        private readonly IAudioManager _audioManager;
        private IAudioRecorder? _recorder;
        private CancellationTokenSource? _cts;
        private System.Threading.Timer? _speechTimer;
        private readonly TimeSpan _speechTimeout = TimeSpan.FromSeconds(1.5);

        public bool IsRecording => _recorder?.IsRecording ?? false;

        public STTService(IAudioManager audioManager, int port) : base(ServiceType.STT, port)
        {
            _audioManager = audioManager;
        }

        public async Task<List<string>?> GetAvailableModelsAsync()
        {
            return await GetModelsAsync("/stt/models");
        }

        public async Task<bool> LoadModelAsync(string modelName, Action<string>? onProgress = null)
        {
            string modelPath = Path.Combine(AppContext.BaseDirectory, "Python", "stt_models", modelName);
            var payload = new { path = modelPath };
            return await LoadModelStreamingAsync("/stt/load", payload, onProgress);
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

            try
            {
                await _recorder.StartAsync();
                RaiseEvent(ServiceEventType.Info, "Recording started.");
                await Task.Delay(500); 

                _speechTimer = new System.Threading.Timer(OnSpeechTimeout, null, Timeout.Infinite, Timeout.Infinite);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (_recorder != null && !_recorder.IsRecording)
                            await Task.Delay(100, _cts.Token);

                        RaiseEvent(ServiceEventType.Info, "Streaming audio to server.");

                        await TranscribeLiveAsync(tempFile, partial =>
                        {
                            RaiseEvent(ServiceEventType.TokenReceived, partial);
                            ResetSpeechTimer();
                        }, _cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        RaiseEvent(ServiceEventType.Info, "Streaming cancelled.");
                    }
                    catch (Exception ex)
                    {
                        RaiseEvent(ServiceEventType.Error, $"Streaming error: {ex.Message}");
                    }
                }, _cts.Token);
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Error starting recording: {ex.Message}");
            }
        }

        public async Task StopListeningAsync()
        {
            if (_recorder == null)
                return;

            try
            {
                _cts?.Cancel();
                await _recorder.StopAsync();
                RaiseEvent(ServiceEventType.Info, "Recording stopped.");
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Error during stop: {ex.Message}");
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
                RaiseEvent(ServiceEventType.Error, $"Failed to stop recorder: {ex.Message}");
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

                RaiseEvent(ServiceEventType.RequestSent, "Sending stream request...");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = await ErrorHandler.GetErrorMessageFromResponse(response);
                    RaiseEvent(ServiceEventType.Error, $"Stream API error: {errorMessage}");
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        onPartialReceived(line);
                    ResetSpeechTimer();
                }
            }
            catch (HttpRequestException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Network error during streaming: {ex.Message}");
            }
            catch (JsonException ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Error parsing JSON during streaming: {ex.Message}");
            }
            catch (Exception ex)
            {
                RaiseEvent(ServiceEventType.Error, $"Unexpected error during streaming: {ex.Message}");
            }
        }

        private void ResetSpeechTimer()
        {
            _speechTimer?.Change(_speechTimeout, Timeout.InfiniteTimeSpan);
        }

        private void OnSpeechTimeout(object? state)
        {
            RaiseEvent(ServiceEventType.Info, "End of speech detected.");
            StopListeningAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}