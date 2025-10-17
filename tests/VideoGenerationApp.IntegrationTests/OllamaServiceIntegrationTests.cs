using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using VideoGenerationApp.Dto;
using VideoGenerationApp.IntegrationTests.Infrastructure;
using VideoGenerationApp.Services;
using Xunit;
using System.Net;

namespace VideoGenerationApp.IntegrationTests
{
    /// <summary>
    /// Full end-to-end integration tests for Ollama service
    /// These tests verify that all UI input variations are properly formatted and sent to the Ollama API
    /// </summary>
    public class OllamaServiceIntegrationTests
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly IOllamaService _ollamaService;

        public OllamaServiceIntegrationTests()
        {
            _mockHandler = new MockHttpMessageHandler();

            // Setup HttpClient with mock handler
            var httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            // Create logger mock
            var logger = new Mock<ILogger<OllamaService>>();

            // Create real OllamaService
            _ollamaService = new OllamaService(httpClient, logger.Object);
        }

        [Fact]
        public async Task GetLocalModelsAsync_CallsCorrectEndpoint()
        {
            // Arrange
            var modelsResponse = @"{
                ""models"": [
                    {
                        ""name"": ""llama3.2:3b"",
                        ""size"": 2000000000,
                        ""digest"": ""sha256:123456"",
                        ""modified_at"": ""2024-01-01T00:00:00Z""
                    },
                    {
                        ""name"": ""qwen2.5:3b"", 
                        ""size"": 1800000000,
                        ""digest"": ""sha256:789012"",
                        ""modified_at"": ""2024-01-02T00:00:00Z""
                    }
                ]
            }";
            _mockHandler.EnqueueJsonResponse(modelsResponse);

            // Act
            var models = await _ollamaService.GetLocalModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/api/tags", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Get, _mockHandler.LastRequest.Method);
            
            // Verify models are sorted by size (smallest first)
            Assert.Equal(2, models.Count);
            Assert.Equal("qwen2.5:3b", models[0]); // Smaller model first
            Assert.Equal("llama3.2:3b", models[1]);
        }

        [Fact]
        public async Task GetLocalModelsWithDetailsAsync_CallsCorrectEndpoint()
        {
            // Arrange
            var modelsResponse = @"{
                ""models"": [
                    {
                        ""name"": ""llama3.2:3b"",
                        ""size"": 2000000000,
                        ""digest"": ""sha256:123456"",
                        ""modified_at"": ""2024-01-01T00:00:00Z"",
                        ""details"": {
                            ""parent_model"": """",
                            ""format"": ""gguf"",
                            ""family"": ""llama"",
                            ""parameter_size"": ""3.2B"",
                            ""quantization_level"": ""Q4_0""
                        }
                    }
                ]
            }";
            _mockHandler.EnqueueJsonResponse(modelsResponse);

            // Act
            var models = await _ollamaService.GetLocalModelsWithDetailsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/api/tags", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Get, _mockHandler.LastRequest.Method);
            
            Assert.Single(models);
            Assert.Equal("llama3.2:3b", models[0].name);
            Assert.Equal(2000000000, models[0].size);
            Assert.NotNull(models[0].details);
            Assert.Equal("llama", models[0].details.family);
        }

        [Fact]
        public async Task SendPromptAsync_SimpleVersion_SendsCorrectRequest()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Generated video scene content""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var model = "llama3.2:3b";
            var prompt = "Create a video about quantum computing";

            // Act
            var response = await _ollamaService.SendPromptAsync(model, prompt);

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/api/generate", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Post, _mockHandler.LastRequest.Method);

            // Verify request body
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            Assert.Equal(model, requestData.GetProperty("model").GetString());
            Assert.False(requestData.GetProperty("stream").GetBoolean());
            
            // Verify the formatted prompt contains the template
            var sentPrompt = requestData.GetProperty("prompt").GetString();
            Assert.Contains("You are a multimodal content generator", sentPrompt);
            Assert.Contains(prompt, sentPrompt);
            
            Assert.Equal("Generated video scene content", response);
        }

        [Fact]
        public async Task SendPromptAsync_WithAllParameters_SendsCompleteRequest()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Complete response with all parameters""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test prompt for all parameters",
                stream = true,
                format = "json",
                keep_alive = "5m",
                max_tokens = 4000,
                temperature = 0.8f,
                top_p = 0.9f,
                num_predictions = 2f
            };

            // Act
            var response = await _ollamaService.SendPromptAsync(request);

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/api/generate", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Post, _mockHandler.LastRequest.Method);

            // Verify all parameters are sent correctly
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal("llama3.2:3b", requestData.GetProperty("model").GetString());
            Assert.Equal("Test prompt for all parameters", requestData.GetProperty("prompt").GetString());
            Assert.True(requestData.GetProperty("stream").GetBoolean());
            Assert.Equal("json", requestData.GetProperty("format").GetString());
            Assert.Equal("5m", requestData.GetProperty("keep_alive").GetString());
            Assert.Equal(4000, requestData.GetProperty("max_tokens").GetInt32());
            Assert.Equal(0.8f, requestData.GetProperty("temperature").GetSingle(), 0.01f);
            Assert.Equal(0.9f, requestData.GetProperty("top_p").GetSingle(), 0.01f);
            Assert.Equal(2f, requestData.GetProperty("num_predictions").GetSingle(), 0.01f);
            
            Assert.Equal("Complete response with all parameters", response);
        }

        [Fact]
        public async Task SendPromptAsync_WithDefaultParameters_UsesCorrectDefaults()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Response with defaults""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "qwen2.5:3b",
                prompt = "Default parameters test"
                // All other parameters should use defaults from OllamaPromptRequest
            };

            // Act
            var response = await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            // Verify default values are applied
            Assert.Equal("qwen2.5:3b", requestData.GetProperty("model").GetString());
            Assert.Equal("Default parameters test", requestData.GetProperty("prompt").GetString());
            Assert.False(requestData.GetProperty("stream").GetBoolean()); // Default: false
            Assert.Equal("json", requestData.GetProperty("format").GetString()); // Default: "json"
            Assert.Equal("3m", requestData.GetProperty("keep_alive").GetString()); // Default: "3m"
            Assert.Equal(8000, requestData.GetProperty("max_tokens").GetInt32()); // Default: 8000
            Assert.Equal(0.3f, requestData.GetProperty("temperature").GetSingle(), 0.01f); // Default: 0.3f
            Assert.Equal(0.7f, requestData.GetProperty("top_p").GetSingle(), 0.01f); // Default: 0.7f
            Assert.Equal(1f, requestData.GetProperty("num_predictions").GetSingle(), 0.01f); // Default: 1f
        }

        [Theory]
        [InlineData(0.0f, 0.1f)] // Very focused
        [InlineData(0.3f, 0.7f)] // Balanced (UI default)
        [InlineData(0.8f, 0.9f)] // Creative
        [InlineData(1.0f, 0.95f)] // Very creative
        [InlineData(2.0f, 1.0f)] // Maximum creativity
        public async Task SendPromptAsync_WithDifferentCreativityLevels_SendsCorrectParameters(float temperature, float topP)
        {
            // Arrange
            var promptResponse = @"{""response"": ""Creative response""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test creativity parameters",
                temperature = temperature,
                top_p = topP
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal(temperature, requestData.GetProperty("temperature").GetSingle(), 0.01f);
            Assert.Equal(topP, requestData.GetProperty("top_p").GetSingle(), 0.01f);
        }

        [Theory]
        [InlineData(1)] // Minimum tokens
        [InlineData(1000)] // Small response
        [InlineData(8000)] // Default max tokens
        [InlineData(16000)] // Large response
        public async Task SendPromptAsync_WithDifferentTokenLimits_SendsCorrectMaxTokens(int maxTokens)
        {
            // Arrange
            var promptResponse = @"{""response"": ""Response with token limit""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test token limits",
                max_tokens = maxTokens
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal(maxTokens, requestData.GetProperty("max_tokens").GetInt32());
        }

        [Theory]
        [InlineData("0")] // Immediate unload
        [InlineData("30s")] // 30 seconds
        [InlineData("3m")] // 3 minutes (default)
        [InlineData("1h")] // 1 hour
        [InlineData("24h")] // 24 hours
        public async Task SendPromptAsync_WithDifferentKeepAliveDurations_SendsCorrectDuration(string keepAlive)
        {
            // Arrange
            var promptResponse = @"{""response"": ""Response with keep alive""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test keep alive settings",
                keep_alive = keepAlive
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal(keepAlive, requestData.GetProperty("keep_alive").GetString());
        }

        [Theory]
        [InlineData(true)] // Streaming enabled
        [InlineData(false)] // Streaming disabled (default)
        public async Task SendPromptAsync_WithStreamingOptions_SendsCorrectStreamFlag(bool stream)
        {
            // Arrange
            var promptResponse = @"{""response"": ""Streaming test response""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test streaming options",
                stream = stream
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal(stream, requestData.GetProperty("stream").GetBoolean());
        }

        [Fact]
        public async Task SendPromptAsync_WithLongPrompt_HandlesLargeInput()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Response to long prompt""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            // Create a very long prompt (over 1000 characters)
            var longPrompt = string.Join(" ", Enumerable.Repeat("This is a very long prompt that tests the system's ability to handle large input text.", 20));
            
            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = longPrompt
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            var sentPrompt = requestData.GetProperty("prompt").GetString();
            Assert.NotNull(sentPrompt);
            Assert.Equal(longPrompt, sentPrompt);
            Assert.True(sentPrompt.Length > 1000);
        }

        [Fact]
        public async Task SendPromptAsync_WithSpecialCharacters_HandlesEncodingCorrectly()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Response with special chars: йс????""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test with special characters: йсья???????????"
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            var sentPrompt = requestData.GetProperty("prompt").GetString();
            Assert.Contains("йсья???????????", sentPrompt);
        }

        [Fact]
        public async Task GetFormattedPrompt_AppliesTemplateCorrectly()
        {
            // Arrange
            var userPrompt = "Explain machine learning basics";

            // Act
            var formattedPrompt = _ollamaService.GetFormattedPrompt(userPrompt);

            // Assert
            Assert.Contains("You are a multimodal content generator", formattedPrompt);
            Assert.Contains("Explain machine learning basics", formattedPrompt);
            Assert.Contains("happy", formattedPrompt); // Default tone
            Assert.Contains("\"narrative\":", formattedPrompt);
            Assert.Contains("\"audio\":", formattedPrompt);
            Assert.Contains("\"video_actions\":", formattedPrompt);
        }

        [Fact]
        public void TryParseVideoSceneOutput_WithValidJSON_ParsesCorrectly()
        {
            // Arrange
            var validJsonResponse = @"{
                ""narrative"": ""Test narrative about AI"",
                ""tone"": ""positive"",
                ""emotion"": ""excited"",
                ""voice_style"": ""enthusiastic female"",
                ""visual_description"": ""Modern tech background"",
                ""video_actions"": [""gesture_right"", ""smile"", ""point_forward""],
                ""audio"": {
                    ""background_music"": ""upbeat electronic"",
                    ""sound_effects"": [""keyboard_typing"", ""notification_chime""],
                    ""audio_mood"": ""energetic"",
                    ""volume_levels"": ""speech: loud, music: medium""
                }
            }";

            // Act
            var parsed = _ollamaService.TryParseVideoSceneOutput(validJsonResponse);

            // Assert
            Assert.NotNull(parsed);
            Assert.Equal("Test narrative about AI", parsed.narrative);
            Assert.Equal("positive", parsed.tone);
            Assert.Equal("excited", parsed.emotion);
            Assert.Equal("enthusiastic female", parsed.voice_style);
            Assert.Equal("Modern tech background", parsed.visual_description);
            Assert.Equal(3, parsed.video_actions.Count);
            Assert.Contains("gesture_right", parsed.video_actions);
            Assert.NotNull(parsed.audio);
            Assert.Equal("upbeat electronic", parsed.audio.background_music);
        }

        [Fact]
        public void TryParseVideoSceneOutput_WithInvalidJSON_ReturnsNull()
        {
            // Arrange
            var invalidResponse = "This is not valid JSON content for parsing";

            // Act
            var parsed = _ollamaService.TryParseVideoSceneOutput(invalidResponse);

            // Assert
            Assert.Null(parsed);
        }

        [Fact]
        public void TryParseVideoSceneOutput_WithPartialJSON_HandlesGracefully()
        {
            // Arrange
            var partialJsonResponse = @"Here is the response: {
                ""narrative"": ""Partial narrative"",
                ""tone"": ""neutral""
            } and some extra text";

            // Act
            var parsed = _ollamaService.TryParseVideoSceneOutput(partialJsonResponse);

            // Assert
            Assert.NotNull(parsed);
            Assert.Equal("Partial narrative", parsed.narrative);
            Assert.Equal("neutral", parsed.tone);
        }

        [Fact]
        public async Task SendPromptAsync_WithNetworkError_ThrowsException()
        {
            // Arrange
            _mockHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error")
            });

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Network error test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _ollamaService.SendPromptAsync(request));
        }

        [Fact]
        public async Task GetLocalModelsAsync_WithNetworkError_ThrowsException()
        {
            // Arrange
            _mockHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _ollamaService.GetLocalModelsAsync());
        }

        [Fact]
        public async Task SendPromptAsync_WithEmptyResponse_HandlesGracefully()
        {
            // Arrange
            var emptyResponse = @"{""response"": """"}";
            _mockHandler.EnqueueJsonResponse(emptyResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Empty response test"
            };

            // Act
            var response = await _ollamaService.SendPromptAsync(request);

            // Assert
            Assert.Equal(string.Empty, response);
        }

        [Theory]
        [InlineData("json")] // Default format from UI
        [InlineData("text")] // Plain text format
        [InlineData("markdown")] // Markdown format
        [InlineData("")] // Empty format
        public async Task SendPromptAsync_WithDifferentFormats_SendsCorrectFormat(string format)
        {
            // Arrange
            var promptResponse = @"{""response"": ""Format test response""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test different formats",
                format = format
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            Assert.Equal(format, requestData.GetProperty("format").GetString());
        }

        [Fact]
        public void FormatFileSize_FormatsCorrectly()
        {
            // Test the static utility method
            Assert.Equal("1.0 KB", OllamaService.FormatFileSize(1024));
            Assert.Equal("1.0 MB", OllamaService.FormatFileSize(1024 * 1024));
            Assert.Equal("1.5 GB", OllamaService.FormatFileSize((long)(1.5 * 1024 * 1024 * 1024)));
            Assert.Equal("500.0 B", OllamaService.FormatFileSize(500));
        }

        /// <summary>
        /// Test that demonstrates parameters that have NO effect on HTTP message
        /// This documents parameters that are UI-only or derived values
        /// </summary>
        [Fact]
        public async Task SendPromptAsync_UIOnlyParameters_HaveNoEffectOnHTTPMessage()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Test response""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Test UI-only parameters"
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert - Verify what is NOT sent to Ollama
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(requestBody);
            
            // These parameters should NOT appear in the HTTP request to Ollama:
            
            // 1. UI model selection dropdown filtering (by size) - has no effect on HTTP
            //    The filtering happens client-side in GetLocalModelsAsync result processing
            
            // 2. UI parameter descriptions/tooltips - have no effect on HTTP
            //    These are purely for user guidance in the UI
            
            // 3. UI validation states (disabled buttons, error messages) - have no effect on HTTP
            //    These are UI state management only
            
            // 4. Model size display formatting - has no effect on HTTP 
            //    FormatFileSize is used only for UI display, not in API calls
            
            // 5. Parsed output state management - has no effect on HTTP
            //    OutputState is for UI display of results, not input to Ollama
            
            // Verify only the core Ollama API parameters are present
            var expectedProperties = new[] { "model", "prompt", "stream", "format", "keep_alive", "max_tokens", "temperature", "top_p", "num_predictions" };
            var actualProperties = requestData.EnumerateObject().Select(p => p.Name).ToArray();
            
            foreach (var expectedProp in expectedProperties)
            {
                Assert.Contains(expectedProp, actualProperties);
            }
            
            // Verify no unexpected UI-only properties leak into the HTTP request
            Assert.Equal(expectedProperties.Length, actualProperties.Length);
        }

        [Fact]
        public async Task SendPromptAsync_WithUIInputValidation_DoesNotAffectHTTPRequest()
        {
            // Arrange
            var promptResponse = @"{""response"": ""Validation test response""}";
            _mockHandler.EnqueueJsonResponse(promptResponse);

            // This demonstrates that UI validation logic (like checking for empty prompts)
            // happens before the HTTP request, so invalid inputs never reach the HTTP layer
            var request = new OllamaPromptRequest
            {
                model = "llama3.2:3b",
                prompt = "Valid prompt after UI validation"
            };

            // Act
            await _ollamaService.SendPromptAsync(request);

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            
            // UI validation effects that have NO impact on HTTP message:
            // - Empty prompt validation (prevents HTTP call entirely)
            // - Model selection validation (prevents HTTP call entirely) 
            // - Loading states and button disable states (UI only)
            // - Error message display (UI only)
            // - Parameter range validation (enforced by UI controls, not sent to API)
            
            // The HTTP request contains only the valid, processed parameters
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.Contains("Valid prompt after UI validation", requestBody);
        }
    }
}