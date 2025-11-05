using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using VideoGenerationApp.Dto;
using static System.Collections.Specialized.BitVector32;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VideoGenerationApp.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;

        private string _promptTemplate = @"You are a multimodal content generator. Your task is to produce a structured JSON object that describes a short video scene based on the given topic and tone.

The output must include the following fields:
- ""audio"": An object containing audio-specific instructions and preferences:
  - ""lyrics"": Text of any spoken words or dialogue.
  - ""tags"": List of keywords or phrases to enhance audio quality.
- ""video"": An object containing video-specific instructions:
  - ""positive_prompt"": Text of what will happen on screen.
  - ""negative_prompt"": Text of what to avoid in the visuals.
- ""image"": An object containing image-specific instructions:
  - ""positive_prompt"": Text of what is in the image.
  - ""negative_prompt"": Text of what to avoid in the image.

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

        public async Task<List<string>> GetLocalModelsAsync()
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

        public async Task<List<OllamaModel>> GetLocalModelsWithDetailsAsync()
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

        public async Task<string> SendPromptAsync(OllamaPromptRequest request)
        {
            var result = await SendPromptWithTimingAsync(request);
            return result.ResponseText;
        }

        public async Task<OllamaResponseWithTiming> SendPromptWithTimingAsync(OllamaPromptRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Ensure stream is false for non-streaming response handling
                request.stream = false;

                var payload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                _logger.LogInformation("Sending generate request to Ollama: model={Model}, stream={Stream}, format={Format}",
                    request.model, request.stream, request.format);

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

                var result = JsonSerializer.Deserialize<OllamaPromptResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogWarning("Ollama returned null response");
                    return new OllamaResponseWithTiming { ResponseText = string.Empty };
                }

                stopwatch.Stop();
                
                // Store execution time in the result for display
                result.execution_time_ms = stopwatch.ElapsedMilliseconds;
                
                var responseText = result.response ?? string.Empty;
                
                _logger.LogInformation("Ollama response: done={Done}, done_reason={DoneReason}, eval_count={EvalCount}, total_duration={TotalDuration}ns, execution_time={ExecutionTime}ms",
                    result.done, result.done_reason, result.eval_count, result.total_duration, result.execution_time_ms);

                return new OllamaResponseWithTiming
                {
                    ResponseText = responseText,
                    FullResponse = result
                };
            }
            catch (JsonException jex)
            {
                stopwatch.Stop();
                _logger.LogError(jex, "JSON parse error from Ollama generate after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error sending prompt to Ollama after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
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
