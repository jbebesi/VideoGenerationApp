using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Services
{
    public class OllamaServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<OllamaService>> _loggerMock;
        private readonly OllamaService _ollamaService;

        public OllamaServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };
            _loggerMock = new Mock<ILogger<OllamaService>>();
            _ollamaService = new OllamaService(_httpClient, _loggerMock.Object);
        }

        [Fact]
        public async Task GetLocalModelsAsync_ReturnsModelsOrderedBySize_WhenSuccessful()
        {
            // Arrange
            var mockResponse = new OllamaTagsResponse
            {
                models = new List<OllamaModel>
                {
                    new OllamaModel { name = "llama3:8b", size = 4600000000 },
                    new OllamaModel { name = "gemma:2b", size = 1500000000 },
                    new OllamaModel { name = "llama3:70b", size = 35000000000 }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/tags")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _ollamaService.GetLocalModelsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("gemma:2b", result[0]); // Smallest first
            Assert.Equal("llama3:8b", result[1]);
            Assert.Equal("llama3:70b", result[2]); // Largest last
        }

        [Fact]
        public async Task GetLocalModelsAsync_ReturnsEmptyList_WhenNoModels()
        {
            // Arrange
            var mockResponse = new OllamaTagsResponse { models = new List<OllamaModel>() };
            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _ollamaService.GetLocalModelsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetLocalModelsWithDetailsAsync_ReturnsModelsWithDetails_WhenSuccessful()
        {
            // Arrange
            var mockResponse = new OllamaTagsResponse
            {
                models = new List<OllamaModel>
                {
                    new OllamaModel { name = "llama3:8b", size = 4600000000, digest = "abc123", modified_at = DateTime.UtcNow }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(mockResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _ollamaService.GetLocalModelsWithDetailsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("llama3:8b", result[0].name);
            Assert.Equal(4600000000, result[0].size);
        }

        [Fact]
        public async Task SendPromptAsync_WithModelAndPrompt_ReturnsResponse_WhenSuccessful()
        {
            // Arrange
            var model = "llama3:8b";
            var prompt = "Hello world";
            var expectedResponse = "Hello! How can I help you today?";

            var mockOllamaResponse = new OllamaPromptResponse
            {
                response = expectedResponse
            };

            var jsonResponse = JsonSerializer.Serialize(mockOllamaResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri!.ToString().Contains("/api/generate") &&
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _ollamaService.SendPromptAsync(model, prompt);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task SendPromptAsync_WithRequest_ReturnsResponse_WhenSuccessful()
        {
            // Arrange
            var request = new OllamaPromptRequest
            {
                model = "llama3:8b",
                prompt = "Hello world",
                stream = false,
                temperature = 0.7f,
                max_tokens = 100
            };

            var expectedResponse = "Hello! How can I help you today?";
            var mockOllamaResponse = new OllamaPromptResponse
            {
                response = expectedResponse
            };

            var jsonResponse = JsonSerializer.Serialize(mockOllamaResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _ollamaService.SendPromptAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task SendPromptAsync_ThrowsException_WhenHttpRequestFails()
        {
            // Arrange
            var request = new OllamaPromptRequest
            {
                model = "llama3:8b",
                prompt = "Hello world",
                stream = false
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error", Encoding.UTF8, "text/plain")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _ollamaService.SendPromptAsync(request));
        }

        [Fact]
        public void GetFormattedPrompt_ReplacesPlaceholders_WhenCalled()
        {
            // Arrange
            var userPrompt = "Create a video about cats";

            // Act
            var result = _ollamaService.GetFormattedPrompt(userPrompt);

            // Assert
            Assert.Contains("Create a video about cats", result);
            Assert.Contains("happy", result); // Default tone
            Assert.DoesNotContain("{{Insert topic here}}", result);
            Assert.DoesNotContain("{{Insert tone here}}", result);
        }

        [Fact]
        public void TryParseVideoSceneOutput_ReturnsValidObject_WhenValidJson()
        {
            // Arrange
            var validJsonResponse = @"{
                ""narrative"": ""A cheerful story about friendship"",
                ""tone"": ""positive"",
                ""emotion"": ""cheerful"",
                ""voice_style"": ""female, warm"",
                ""visual_description"": ""Sunny park with colorful flowers"",
                ""video_actions"": [""wave"", ""smile"", ""point to flowers""],
                ""audio"": {
                    ""background_music"": ""upbeat instrumental"",
                    ""sound_effects"": [""birds chirping"", ""wind rustling""],
                    ""audio_mood"": ""energetic"",
                    ""volume_levels"": ""speech: loud, music: soft""
                }
            }";

            // Act
            var result = _ollamaService.TryParseVideoSceneOutput(validJsonResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("A cheerful story about friendship", result.narrative);
            Assert.Equal("positive", result.tone);
            Assert.Equal("cheerful", result.emotion);
            Assert.Equal("female, warm", result.voice_style);
            Assert.Equal("Sunny park with colorful flowers", result.visual_description);
            Assert.Equal(3, result.video_actions?.Count);
            Assert.NotNull(result.audio);
            Assert.Equal("upbeat instrumental", result.audio.background_music);
        }

        [Fact]
        public void TryParseVideoSceneOutput_ReturnsNull_WhenInvalidJson()
        {
            // Arrange
            var invalidJsonResponse = "This is not a valid JSON response";

            // Act
            var result = _ollamaService.TryParseVideoSceneOutput(invalidJsonResponse);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryParseVideoSceneOutput_ReturnsNull_WhenNoJsonFound()
        {
            // Arrange
            var responseWithoutJson = "Here is some text without any JSON structure at all.";

            // Act
            var result = _ollamaService.TryParseVideoSceneOutput(responseWithoutJson);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(1024, "1.0 KB")]
        [InlineData(1048576, "1.0 MB")]
        [InlineData(1073741824, "1.0 GB")]
        [InlineData(500, "500.0 B")]
        [InlineData(1536, "1.5 KB")]
        public void FormatFileSize_ReturnsCorrectFormat_ForVariousSizes(long bytes, string expected)
        {
            // Act
            var result = OllamaService.FormatFileSize(bytes);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetLocalModelsAsync_ThrowsException_WhenHttpClientThrows()
        {
            // Arrange
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _ollamaService.GetLocalModelsAsync());
        }

        [Fact]
        public async Task GetLocalModelsWithDetailsAsync_ThrowsException_WhenHttpClientThrows()
        {
            // Arrange
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _ollamaService.GetLocalModelsWithDetailsAsync());
        }

        private void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}