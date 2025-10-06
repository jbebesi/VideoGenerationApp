using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Integration
{
    public class AudioGenerationWorkflowTests
    {
        private readonly Mock<ILogger<OllamaService>> _mockOllamaLogger;
        private readonly Mock<ILogger<ComfyUIAudioService>> _mockComfyUILogger;
        private readonly Mock<ILogger<GenerationQueueService>> _mockQueueLogger;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly ServiceCollection _services;

        public AudioGenerationWorkflowTests()
        {
            _mockOllamaLogger = new Mock<ILogger<OllamaService>>();
            _mockComfyUILogger = new Mock<ILogger<ComfyUIAudioService>>();
            _mockQueueLogger = new Mock<ILogger<GenerationQueueService>>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _services = new ServiceCollection();
        }

        [Fact]
        public async Task CompleteAudioGenerationWorkflow_WorksCorrectly_WithMockedServices()
        {
            // Arrange
            var ollamaService = CreateMockedOllamaService();
            var comfyUIService = CreateMockedComfyUIService();
            
            // Setup service scope mocking for queue service
            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            
            serviceProviderMock.Setup(x => x.GetService(typeof(ComfyUIAudioService)))
                .Returns(comfyUIService);
            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            var queueService = new GenerationQueueService(_mockScopeFactory.Object, _mockQueueLogger.Object);

            var userPrompt = "Create upbeat pop music with female vocals";
            var expectedVideoScene = new VideoSceneOutput
            {
                narrative = "An upbeat pop song with energetic female vocals",
                tone = "positive",
                emotion = "excited",
                voice_style = "female, energetic",
                audio = new AudioSection
                {
                    background_music = "upbeat pop instrumental",
                    audio_mood = "energetic",
                    sound_effects = new List<string> { "synthesizer", "drums" }
                }
            };

            // Act
            // Step 1: Generate video scene with Ollama
            var formattedPrompt = ollamaService.GetFormattedPrompt(userPrompt);
            var ollamaResponse = await ollamaService.SendPromptAsync("test-model", formattedPrompt);
            var videoScene = ollamaService.TryParseVideoSceneOutput(ollamaResponse);

            // Step 2: Queue audio generation task
            var taskId = await queueService.QueueGenerationAsync("Test Audio Generation", new AudioWorkflowConfig());

            // Step 3: Generate audio with ComfyUI (this will return null since health check fails)
            var audioFile = await comfyUIService.GenerateAsync(expectedVideoScene);

            // Assert
            Assert.NotNull(formattedPrompt);
            Assert.Contains(userPrompt, formattedPrompt);
            Assert.NotNull(ollamaResponse);
            Assert.NotNull(videoScene);
            Assert.Equal("positive", videoScene.tone);
            Assert.Equal("excited", videoScene.emotion);
            Assert.NotNull(videoScene.audio);
            Assert.Equal("upbeat pop instrumental", videoScene.audio.background_music);

            Assert.NotNull(taskId);
            Assert.NotEmpty(taskId);

            // Audio file will be null since ComfyUI health check fails with mocked service
            Assert.Null(audioFile);

            var task = queueService.GetTask(taskId);
            Assert.NotNull(task);
            Assert.Equal("Test Audio Generation", task.Name);
        }

        [Fact]
        public void AudioWorkflowFactory_CreatesValidWorkflow_ForDifferentGenres()
        {
            // Arrange
            var popConfig = new AudioWorkflowConfig
            {
                Tags = "pop, female voice, catchy",
                Lyrics = "[verse]\nDancing through the night\n[chorus]\nFeel the music bright",
                AudioDurationSeconds = 180
            };

            var rockConfig = new AudioWorkflowConfig
            {
                Tags = "rock, male voice, powerful",
                Lyrics = "[verse]\nRocking all night long\n[chorus]\nWe are strong",
                AudioDurationSeconds = 240
            };

            // Act
            var popWorkflow = AudioWorkflowFactory.CreateWorkflow(popConfig);
            var rockWorkflow = AudioWorkflowFactory.CreateWorkflow(rockConfig);

            // Assert
            Assert.NotNull(popWorkflow);
            Assert.NotNull(rockWorkflow);
            Assert.NotEqual(popWorkflow.id, rockWorkflow.id);

            // Verify both workflows have required nodes
            foreach (var workflow in new[] { popWorkflow, rockWorkflow })
            {
                Assert.Contains(workflow.nodes, n => n.type == "TextEncodeAceStepAudio");
                Assert.Contains(workflow.nodes, n => n.type == "EmptyAceStepLatentAudio");
                Assert.Contains(workflow.nodes, n => n.type == "KSampler");
                Assert.Contains(workflow.nodes, n => n.type == "VAEDecodeAudio");
            }

            // Verify configuration differences
            var popTextNode = popWorkflow.nodes.First(n => n.type == "TextEncodeAceStepAudio");
            var rockTextNode = rockWorkflow.nodes.First(n => n.type == "TextEncodeAceStepAudio");

            Assert.Contains("pop", popTextNode.widgets_values[0].ToString()!);
            Assert.Contains("rock", rockTextNode.widgets_values[0].ToString()!);
        }

        [Fact]
        public async Task GenerationQueue_HandlesMultipleTasks_Correctly()
        {
            // Arrange
            // Setup service scope mocking to avoid NullReferenceException
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8188")
            };

            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.WebRootPath).Returns("C:\\TestWebRoot");

            var mockSettings = Mock.Of<IOptions<ComfyUISettings>>(o => 
                o.Value == new ComfyUISettings { ApiUrl = "http://localhost:8188", TimeoutMinutes = 10 });

            // Mock the response for SubmitWorkflowAsync
            var mockSubmitResponse = new ComfyUIWorkflowResponse
            {
                prompt_id = "test-prompt-id-123",
                number = 1
            };

            var jsonResponse = JsonSerializer.Serialize(mockSubmitResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri!.ToString().Contains("/prompt") &&
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var mockComfyUIService = new ComfyUIAudioService(
                httpClient,
                _mockComfyUILogger.Object,
                mockEnvironment.Object,
                mockSettings);

            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            
            serviceProviderMock.Setup(x => x.GetService(typeof(ComfyUIAudioService)))
                .Returns(mockComfyUIService);
            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            var queueService = new GenerationQueueService(_mockScopeFactory.Object, _mockQueueLogger.Object);

            var config1 = new AudioWorkflowConfig { Tags = "pop, upbeat" };
            var config2 = new AudioWorkflowConfig { Tags = "rock, intense" };
            var config3 = new AudioWorkflowConfig { Tags = "jazz, smooth" };

            // Act
            var taskId1 = await queueService.QueueGenerationAsync("Pop Song", config1);
            var taskId2 = await queueService.QueueGenerationAsync("Rock Song", config2);
            var taskId3 = await queueService.QueueGenerationAsync("Jazz Song", config3);

            var allTasks = queueService.GetAllTasks().ToList();

            // Assert
            Assert.Equal(3, allTasks.Count);
            Assert.Contains(allTasks, t => t.Name == "Pop Song");
            Assert.Contains(allTasks, t => t.Name == "Rock Song");
            Assert.Contains(allTasks, t => t.Name == "Jazz Song");

            // All should be in Queued status after successful submission
            Assert.All(allTasks, t => Assert.Equal(GenerationStatus.Queued, t.Status));

            // Test task cancellation
            var cancelResult = queueService.CancelTask(taskId2);
            Assert.True(cancelResult);

            var cancelledTask = queueService.GetTask(taskId2);
            Assert.Equal(GenerationStatus.Cancelled, cancelledTask!.Status);
        }

        [Fact]
        public async Task ErrorHandling_WorksCorrectly_ThroughoutWorkflow()
        {
            // Arrange
            var failingOllamaService = CreateFailingOllamaService();
            var comfyUIService = CreateMockedComfyUIService();

            // Act & Assert
            // Test Ollama service failure
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                failingOllamaService.GetLocalModelsAsync());

            // Test invalid video scene parsing
            var invalidResponse = "This is not valid JSON";
            var parsedScene = failingOllamaService.TryParseVideoSceneOutput(invalidResponse);
            Assert.Null(parsedScene);

            // Test ComfyUI configuration updates
            var testConfig = new AudioWorkflowConfig { Tags = "test" };
            comfyUIService.SetWorkflowConfig(testConfig);
            var retrievedConfig = comfyUIService.GetWorkflowConfig();
            Assert.Equal("test", retrievedConfig.Tags);
        }

        [Fact]
        public void ServiceIntegration_WorksCorrectly_WithDependencyInjection()
        {
            // Arrange
            _services.AddLogging();
            _services.AddSingleton<HttpClient>();
            _services.AddSingleton<OllamaService>();
            _services.AddSingleton<IOptions<ComfyUISettings>>(provider => 
                Options.Create(new ComfyUISettings { ApiUrl = "http://localhost:8188" }));

            using var serviceProvider = _services.BuildServiceProvider();

            // Act
            var ollamaService = serviceProvider.GetService<OllamaService>();
            var comfyUISettings = serviceProvider.GetService<IOptions<ComfyUISettings>>();

            // Assert
            Assert.NotNull(ollamaService);
            Assert.NotNull(comfyUISettings);
            Assert.Equal("http://localhost:8188", comfyUISettings.Value.ApiUrl);
        }

        [Theory]
        [InlineData("pop", "female voice", 120)]
        [InlineData("rock", "male voice", 180)]
        [InlineData("jazz", "smooth saxophone", 240)]
        [InlineData("classical", "orchestral", 300)]
        public void AudioWorkflowConfiguration_HandlesVariousGenres_Correctly(string genre, string style, int duration)
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Tags = $"{genre}, {style}",
                AudioDurationSeconds = duration,
                Steps = 50,
                CFGScale = 7.5f
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.NotNull(workflow);
            Assert.NotEmpty(workflow.nodes);

            var textEncodeNode = workflow.nodes.First(n => n.type == "TextEncodeAceStepAudio");
            Assert.Contains(genre, textEncodeNode.widgets_values[0].ToString()!);

            var latentAudioNode = workflow.nodes.First(n => n.type == "EmptyAceStepLatentAudio");
            Assert.Equal((float)duration, latentAudioNode.widgets_values[0]);

            var samplerNode = workflow.nodes.First(n => n.type == "KSampler");
            Assert.Equal(50, samplerNode.widgets_values[2]); // Steps
            Assert.Equal(7.5f, samplerNode.widgets_values[3]); // CFG Scale
        }

        private OllamaService CreateMockedOllamaService()
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            // Mock the response for SendPromptAsync
            var mockOllamaResponse = new OllamaPromptResponse
            {
                response = @"{
                    ""narrative"": ""An upbeat pop song with energetic female vocals"",
                    ""tone"": ""positive"",
                    ""emotion"": ""excited"",
                    ""voice_style"": ""female, energetic"",
                    ""visual_description"": ""Bright stage with colorful lights"",
                    ""video_actions"": [""dance"", ""sing"", ""wave""],
                    ""audio"": {
                        ""background_music"": ""upbeat pop instrumental"",
                        ""audio_mood"": ""energetic"",
                        ""sound_effects"": [""synthesizer"", ""drums""]
                    }
                }"
            };

            var jsonResponse = JsonSerializer.Serialize(mockOllamaResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri!.ToString().Contains("/api/generate") &&
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var service = new OllamaService(httpClient, _mockOllamaLogger.Object);
            return service;
        }

        private ComfyUIAudioService CreateMockedComfyUIService()
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8188")
            };

            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.WebRootPath).Returns("C:\\TestWebRoot");

            var mockSettings = Mock.Of<IOptions<ComfyUISettings>>(o => 
                o.Value == new ComfyUISettings { ApiUrl = "http://localhost:8188", TimeoutMinutes = 10 });

            var service = new ComfyUIAudioService(
                httpClient,
                _mockComfyUILogger.Object,
                mockEnvironment.Object,
                mockSettings);

            return service;
        }

        private OllamaService CreateFailingOllamaService()
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            // Mock the handler to throw HttpRequestException
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Mocked service failure"));

            var service = new OllamaService(httpClient, _mockOllamaLogger.Object);
            return service;
        }
    }
}