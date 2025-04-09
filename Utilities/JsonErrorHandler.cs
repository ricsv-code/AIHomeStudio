using Newtonsoft.Json;

namespace AIHomeStudio.Utilities
{
    public static class JsonErrorHandler
    {
        public static async Task<string> GetErrorMessageFromResponse(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorObject = JsonConvert.DeserializeObject<ErrorResponse>(content);
                return errorObject?.Detail ?? "Unknown error.";
            }
            catch
            {
                return response.ReasonPhrase ?? "Unknown error.";
            }
        }

        private class ErrorResponse
        {
            public string Detail { get; set; }
        }
    }
}