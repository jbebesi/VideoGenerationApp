using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace VideoGenerationApp.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<string>> GetLocalModelsAsync()
        {
            try
            {
                _logger.LogInformation("Requesting Ollama tags at {Url}", _httpClient.BaseAddress + "/api/tags");
                var response = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>("/api/tags");
                var list = response?.models?.Select(m => m.name).ToList() ?? new List<string>();
                _logger.LogInformation("Ollama returned {Count} models", list.Count);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Ollama tags");
                throw;
            }
        }

        public async Task<string> SendPromptAsync(string model, string prompt)
        {
            try
            {
                var request = new OllamaPromptRequest { model = model, prompt = prompt, stream = false };
                var payload = JsonSerializer.Serialize(request);
                _logger.LogInformation("Sending generate request to Ollama: model={Model}, length={Length}", model, prompt?.Length);

                var httpResponse = await _httpClient.PostAsync("/api/generate",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errBody = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Ollama /api/generate failed: {Status} {Reason}. Body: {Body}", (int)httpResponse.StatusCode, httpResponse.ReasonPhrase, errBody);
                    httpResponse.EnsureSuccessStatusCode();
                }

                var json = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("Ollama raw response: {Json}", json);
                var result = JsonSerializer.Deserialize<OllamaPromptResponse>(json);
                var text = result?.response ?? string.Empty;
                _logger.LogInformation("Ollama response length: {Length}", text.Length);
                return text;
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "JSON parse error from Ollama generate");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending prompt to Ollama");
                throw;
            }
        }

        public class OllamaTagsResponse
        {
            public List<OllamaModel> models { get; set; } = new();
        }
        public class OllamaModel
        {
            public string name { get; set; } = string.Empty;
        }
        public class OllamaPromptRequest
        {
            public string model { get; set; } = string.Empty;
            public string prompt { get; set; } = string.Empty;
            public bool stream { get; set; } = false;
        }
        public class OllamaPromptResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
