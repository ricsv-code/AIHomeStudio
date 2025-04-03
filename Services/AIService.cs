using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AIHomeStudio.Services
{
    public class AIService : ServiceBase
    {
        private static readonly HttpClient _httpClient = new();
        private string _baseUrl = "http://localhost:8000";

        public AIService(int port) : base(ServiceType.AI)
        {
            _baseUrl = $"http://localhost:{port}";
        }

        public async Task AskAIStreamedAsync(
            string prompt = "",
            string systemPrompt = "",
            float temperature = 0.6f,
            float topP = 0.6f,
            int maxTokens = 100)
        {
            var payload = new
            {
                prompt,
                system_prompt = systemPrompt,
                temperature,
                top_p = topP,
                max_new_tokens = maxTokens
            };

            var json = JsonConvert.SerializeObject(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/generate_stream")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            RaiseEvent(ServiceEventType.RequestSent, "/generate_stream");


            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                RaiseEvent(ServiceEventType.Error, "[AI] Generative stream request failed.");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            char[] buffer = new char[1];
            while (!reader.EndOfStream)
            {
                int read = await reader.ReadAsync(buffer, 0, 1);
                if (read > 0)
                {
                    RaiseEvent(ServiceEventType.TokenReceived, buffer[0].ToString());
                }
            }

            RaiseEvent(ServiceEventType.TokenReceived, "[END]");
        }

        public async Task<bool> LoadModelAsync(string modelPath)
        {
            var payload = new { path = modelPath };
            var json = JsonConvert.SerializeObject(payload);

            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/load_model")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            RaiseEvent(ServiceEventType.RequestSent, "/load_model");


            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                RaiseEvent(ServiceEventType.Error, "[AI] Model failed to load.");
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

        public async Task<bool> CheckModelStatusAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/model/status");
            if (!response.IsSuccessStatusCode)
                return false;

            RaiseEvent(ServiceEventType.RequestSent, "/model/status");

            var result = await response.Content.ReadAsStringAsync();
            dynamic parsed = JsonConvert.DeserializeObject(result);
            return parsed?.loaded == true;
        }
    }
}
