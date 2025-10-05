using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;

        private string _promptTemplate = @"You are a multimodal content generator. Your task is to produce a structured JSON object that describes a short video scene based on the given topic and tone.

The output must include the following fields:
- ""narrative"": A short paragraph describing the core message or story.
- ""tone"": The emotional tone (e.g., positive, negative, neutral).
- ""emotion"": The dominant emotion expressed by the speaker (e.g., cheerful, angry, calm).
- ""voice_style"": Description of the speaker's voice (e.g., male, deep, sarcastic).
- ""visual_description"": What the scene should look like (e.g., background, avatar appearance).
- ""video_actions"": A list of 2–5 actions or gestures the avatar should perform.
- ""audio"": An object containing audio-specific instructions and preferences:
  - ""background_music"": Type of background music or ambient sounds (e.g., ""upbeat instrumental"", ""nature sounds"", ""silence"").
  - ""sound_effects"": List of sound effects to accompany actions (e.g., [""applause"", ""footsteps"", ""door closing""]).
  - ""audio_mood"": Overall audio atmosphere (e.g., ""energetic"", ""calm"", ""mysterious"").
  - ""volume_levels"": Relative volume guidance (e.g., ""speech: loud, music: soft, effects: medium"").

Constraints:
- Ensure all fields are filled.
- Format the output as valid JSON.
- The audio section should complement the visual and narrative elements.

Topic: {{Insert topic here}}
Tone: {{Insert tone here}}

Generate the structured output now.
";

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public virtual async Task<List<string>> GetLocalModelsAsync()
        {
            try
            {
                _logger.LogInformation("Requesting Ollama tags at {Url}", _httpClient.BaseAddress + "/api/tags");
                var response = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>("/api/tags");
                
                if (response?.models == null)
                {
                    _logger.LogWarning("No models returned from Ollama API");
                    return new List<string>();
                }
                
                // Sort models by size (smallest first) and return just the names
                var list = response.models
                    .Where(m => !string.IsNullOrEmpty(m.name))
                    .OrderBy(m => m.size)
                    .Select(m => m.name)
                    .ToList();
                    
                _logger.LogInformation("Ollama returned {Count} models, sorted by size", list.Count);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Ollama tags");
                throw;
            }
        }

        public virtual async Task<List<OllamaModel>> GetLocalModelsWithDetailsAsync()
        {
            try
            {
                _logger.LogInformation("Requesting Ollama tags with details at {Url}", _httpClient.BaseAddress + "/api/tags");
                
                var json = await _httpClient.GetStringAsync("/api/tags");
                _logger.LogDebug("Raw Ollama response: {Json}", json);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };
                
                var response = JsonSerializer.Deserialize<OllamaTagsResponse>(json, options);
                
                if (response?.models == null)
                {
                    _logger.LogWarning("No models returned from Ollama API");
                    return new List<OllamaModel>();
                }
                
                // Sort models by size (smallest first) and filter out invalid entries
                var list = response.models
                    .Where(m => !string.IsNullOrEmpty(m.name))
                    .OrderBy(m => m.size)
                    .ToList();
                    
                _logger.LogInformation("Ollama returned {Count} models with details, sorted by size", list.Count);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Ollama tags with details");
                throw;
            }
        }

        public virtual async Task<string> SendPromptAsync(string model, string prompt)
        {
            var request = new OllamaPromptRequest 
            { 
                model = model, 
                prompt = GetFormattedPrompt(prompt), 
                stream = false 
            };
            return await SendPromptAsync(request);
        }

        public virtual async Task<string> SendPromptAsync(OllamaPromptRequest request)
        {
            try
            {
                var payload = JsonSerializer.Serialize(request);
                _logger.LogInformation("Sending generate request to Ollama: model={Model}, tokens={MaxTokens}, temp={Temperature}", 
                    request.model, request.max_tokens, request.temperature);

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

        public string GetFormattedPrompt(string userPrompt)
        {
            return _promptTemplate
                .Replace("{{Insert topic here}}", userPrompt)
                .Replace("{{Insert tone here}}", "happy");
        }

        public VideoSceneOutput? TryParseVideoSceneOutput(string rawResponse)
        {
            try
            {
                // Try to find JSON content in the response
                var trimmed = rawResponse.Trim();
                
                // Look for JSON block markers
                var jsonStart = trimmed.IndexOf('{');
                var jsonEnd = trimmed.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = JsonSerializer.Deserialize<VideoSceneOutput>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    _logger.LogInformation("Successfully parsed VideoSceneOutput from Ollama response");
                    return parsed;
                }
                else
                {
                    _logger.LogWarning("No valid JSON structure found in Ollama response");
                    return null;
                }
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex, "Failed to parse VideoSceneOutput from Ollama response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing VideoSceneOutput");
                return null;
            }
        }

        public static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
