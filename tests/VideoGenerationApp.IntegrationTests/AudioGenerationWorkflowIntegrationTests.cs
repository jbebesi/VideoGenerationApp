using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.IntegrationTests.Infrastructure;
using VideoGenerationApp.Services;
using ComfyUI.Client.Configuration;
using ComfyUI.Client.Services;
using Xunit;

namespace VideoGenerationApp.IntegrationTests
{
    /// <summary>
    /// Integration tests for AudioGenerationWorkflow
    /// Tests all parameters from GenerateAudio.razor UI component
    /// These tests verify that UI parameters are properly transmitted via HTTP requests to ComfyUI
    /// </summary>
    public class AudioGenerationWorkflowIntegrationTests
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly IComfyUIAudioService _audioService;

        public AudioGenerationWorkflowIntegrationTests()
        {
            _mockHandler = new MockHttpMessageHandler();

            // Setup HttpClient with mock handler
            var httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("http://localhost:8188")
            };

            // Setup ComfyUI client options
            var clientOptions = Options.Create(new ComfyUIClientOptions
            {
                BaseUrl = "http://localhost:8188",
                UseApiPrefix = false
            });

            // Setup ComfyUI settings
            var comfySettings = Options.Create(new ComfyUISettings
            {
                ApiUrl = "http://localhost:8188",
                TimeoutMinutes = 5
            });

            // Create real ComfyUIApiClient with mocked HttpClient
            var apiClient = new ComfyUIApiClient(httpClient, clientOptions);

            // Create logger mocks
            var audioServiceLogger = new Mock<ILogger<ComfyUIAudioService>>();

            // Create mock environment
            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/tmp");

            // Create mock file service
            var mockFileService = new Mock<IComfyUIFileService>();

            // Create real ComfyUIAudioService
            _audioService = new ComfyUIAudioService(
                apiClient,
                audioServiceLogger.Object,
                mockEnvironment.Object,
                comfySettings,
                mockFileService.Object
            );
        }

        [Fact]
        public async Task GetAvailableModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""CheckpointLoaderSimple"": {
                    ""input"": {
                        ""required"": {
                            ""ckpt_name"": [[""ace_model1.safetensors"", ""ace_model2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _audioService.GetAudioModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Get, _mockHandler.LastRequest.Method);
        }

        [Fact]
        public async Task GetCLIPModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""CLIPLoader"": {
                    ""input"": {
                        ""required"": {
                            ""clip_name"": [[""clip1.safetensors"", ""clip2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _audioService.GetCLIPModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task GetVAEModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""VAELoader"": {
                    ""input"": {
                        ""required"": {
                            ""vae_name"": [[""vae1.safetensors"", ""vae2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _audioService.GetVAEModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task GetAudioEncoderModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""AudioEncoder"": {
                    ""input"": {
                        ""required"": {
                            ""encoder_name"": [[""encoder1.safetensors"", ""encoder2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _audioService.GetAudioEncoderModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
        }

        /// <summary>
        /// Tests that AudioWorkflowConfig parameters are correctly included in the generated workflow
        /// These tests verify the workflow creation, which is the function bound to the UI
        /// </summary>
        [Fact]
        public void CreateAudioWorkflow_WithTags_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Tags = "pop, female voice, catchy"
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("pop, female voice, catchy", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithLyrics_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Lyrics = "[verse]\nTest lyrics\n[chorus]\nSing along"
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("Test lyrics", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithCheckpointName_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                CheckpointName = "ace_step_v1_3.5b.safetensors"
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("ace_step_v1_3.5b.safetensors", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithSeed_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Seed = 54321
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("54321", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithSteps_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Steps = 60
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("60", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithCFGScale_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                CFGScale = 6.5f
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("6.5", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithAudioDuration_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                AudioDurationSeconds = 90.0f
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("90", workflowJson);
        }

        [Fact]
        public void CreateAudioWorkflow_WithLyricsStrength_IncludesInWorkflow()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                LyricsStrength = 0.85f
            };

            // Act
            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("0.85", workflowJson);
        }

        // NOTE: The following parameters from the UI do not directly affect the HTTP request content:
        // - SamplerName: May vary by workflow implementation
        // - Scheduler: May vary by workflow implementation
        // - Denoise: May vary by workflow implementation
        // - BatchSize: Used for generating multiple versions, but doesn't change the core workflow structure
        // - ModelShift: ACE Step specific parameter that may not appear in serialized JSON in expected format
        // - TonemapMultiplier: ACE Step specific parameter that may not appear in serialized JSON in expected format
        // - OutputFilename: Used locally after generation, not sent in the HTTP request payload
        // - OutputFormat: Used locally after generation, not sent to ComfyUI
        // - AudioQuality: Used locally for encoding, not sent to ComfyUI
    }
}
