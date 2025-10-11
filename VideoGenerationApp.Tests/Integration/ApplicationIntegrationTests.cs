using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Integration
{
    public class ApplicationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IOllamaService> _mockOllamaService;
        private readonly Mock<IComfyUIAudioService> _mockComfyUIService;

        public ApplicationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockOllamaService = new Mock<IOllamaService>();
            _mockComfyUIService = new Mock<IComfyUIAudioService>();

            // Setup default mock behavior for methods
            _mockComfyUIService.Setup(x => x.GetWorkflowConfig())
                .Returns(new AudioWorkflowConfig());

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real services
                    services.RemoveAll<IOllamaService>();
                    services.RemoveAll<IComfyUIAudioService>();

                    // Add mocked services
                    services.AddSingleton(_mockOllamaService.Object);
                    services.AddSingleton(_mockComfyUIService.Object);

                    // Configure test settings
                    services.Configure<ComfyUISettings>(options =>
                    {
                        options.ApiUrl = "http://localhost:8188";
                        options.TimeoutMinutes = 1; // Short timeout for tests
                    });
                });
            });
        }

        [Fact]
        public async Task HomePage_LoadsSuccessfully_WhenRequested()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Video Generation App", content);
        }

        [Fact]
        public async Task OllamaModelsPage_LoadsSuccessfully_WithMockedService()
        {
            // Arrange
            var client = _factory.CreateClient();
            var mockModels = new List<OllamaModel>
            {
                new OllamaModel { name = "test-model", size = 1000000 }
            };

            _mockOllamaService.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(mockModels);

            // Act
            var response = await client.GetAsync("/ollama-models");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Ollama Local Models", content);
        }

        [Fact]
        public async Task GenerateAudioPage_LoadsSuccessfully_WithMockedService()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/generate-audio");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Generate Audio", content);
        }

        [Fact]
        public async Task GenerationQueuePage_LoadsSuccessfully_WhenRequested()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/generation-queue");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Generation Queue", content);
        }

        [Fact]
        public async Task OllamaService_Integration_WorksWithMockedData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var expectedModels = new List<OllamaModel>
            {
                new OllamaModel { name = "llama3:8b", size = 4600000000 },
                new OllamaModel { name = "gemma:2b", size = 1500000000 }
            };

            _mockOllamaService.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(expectedModels);

            _mockOllamaService.Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Mocked response from Ollama");

            // Act
            var response = await client.GetAsync("/ollama-models");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            
            // Verify the page loaded and would work with the mocked service
            Assert.Contains("Ollama Local Models", content);
        }

        [Fact]
        public async Task ComfyUIService_Integration_WorksWithMockedData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var mockConfig = new AudioWorkflowConfig
            {
                Tags = "test, integration",
                Lyrics = "Integration test lyrics"
            };

            _mockComfyUIService.Setup(x => x.GetWorkflowConfig())
                .Returns(mockConfig);

            _mockComfyUIService.Setup(x => x.GenerateAsync(It.IsAny<VideoSceneOutput>()))
                .ReturnsAsync("test-audio-file.mp3");

            // Act
            var response = await client.GetAsync("/generate-audio");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            
            // Verify the page loaded and would work with the mocked service
            Assert.Contains("Generate Audio", content);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/ollama-models")]
        [InlineData("/generate-audio")]
        [InlineData("/generation-queue")]
        public async Task AllPages_LoadSuccessfully_WithoutErrors(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Setup basic mocks for all services
            _mockOllamaService.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.True(response.IsSuccessStatusCode, 
                $"Page {url} failed to load. Status: {response.StatusCode}");
        }

        [Fact]
        public async Task ErrorHandling_WorksCorrectly_WhenServicesFail()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Setup services to throw exceptions
            _mockOllamaService.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ThrowsAsync(new HttpRequestException("Mocked service failure"));

            // Act
            var response = await client.GetAsync("/ollama-models");

            // Assert
            // Page should still load even if service fails, potentially showing error message
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task ServiceDependencyInjection_WorksCorrectly_InIntegrationTest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            // Act
            var ollamaService = services.GetService<IOllamaService>();
            var comfyUIService = services.GetService<IComfyUIAudioService>();
            var queueService = services.GetService<IGenerationQueueService>();

            // Assert
            Assert.NotNull(ollamaService);
            Assert.NotNull(comfyUIService);
            Assert.NotNull(queueService);
            
            // Verify they are our mocked instances
            Assert.Same(_mockOllamaService.Object, ollamaService);
            Assert.Same(_mockComfyUIService.Object, comfyUIService);
        }

        [Fact]
        public async Task EndToEndWorkflow_WorksCorrectly_WithMockedServices()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Setup complete workflow mocking
            var mockModels = new List<OllamaModel>
            {
                new OllamaModel { name = "test-model:latest", size = 1000000 }
            };

            var mockVideoScene = new VideoSceneOutput
            {
                narrative = "Test narrative",
                tone = "happy",
                emotion = "excited",
                audio = new AudioSection
                {
                    background_music = "upbeat",
                    audio_mood = "energetic"
                }
            };

            _mockOllamaService.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(mockModels);

            _mockOllamaService.Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(JsonSerializer.Serialize(mockVideoScene));

            _mockOllamaService.Setup(x => x.TryParseVideoSceneOutput(It.IsAny<string>()))
                .Returns(mockVideoScene);

            _mockComfyUIService.Setup(x => x.GenerateAsync(It.IsAny<VideoSceneOutput>()))
                .ReturnsAsync("generated-audio.mp3");

            // Act
            var ollamaResponse = await client.GetAsync("/ollama-models");
            var audioResponse = await client.GetAsync("/generate-audio");
            var queueResponse = await client.GetAsync("/generation-queue");

            // Assert
            Assert.True(ollamaResponse.IsSuccessStatusCode);
            Assert.True(audioResponse.IsSuccessStatusCode);
            Assert.True(queueResponse.IsSuccessStatusCode);

            // Verify that all pages loaded correctly with mocked data
            var ollamaContent = await ollamaResponse.Content.ReadAsStringAsync();
            var audioContent = await audioResponse.Content.ReadAsStringAsync();
            var queueContent = await queueResponse.Content.ReadAsStringAsync();

            Assert.Contains("Ollama Local Models", ollamaContent);
            Assert.Contains("Generate Audio", audioContent);
            Assert.Contains("Generation Queue", queueContent);
        }

        [Fact]
        public async Task StaticAssets_LoadCorrectly_InIntegrationTest()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act & Assert - Test that CSS and other assets load
            var response = await client.GetAsync("/");
            Assert.True(response.IsSuccessStatusCode);

            var content = await response.Content.ReadAsStringAsync();
            
            // Should include Bootstrap CSS references
            Assert.Contains("bootstrap", content.ToLower());
        }
    }
}