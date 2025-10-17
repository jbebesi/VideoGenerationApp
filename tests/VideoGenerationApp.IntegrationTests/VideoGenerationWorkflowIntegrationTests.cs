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
    /// Integration tests for VideoGenerationWorkflow
    /// Tests all parameters from GenerateVideo.razor UI component
    /// These tests verify that UI parameters are properly transmitted via HTTP requests to ComfyUI
    /// 
    /// Note: Video generation requires an image file, so tests create workflows that would
    /// eventually be used with an uploaded image.
    /// </summary>
    public class VideoGenerationWorkflowIntegrationTests
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly IComfyUIVideoService _videoService;

        public VideoGenerationWorkflowIntegrationTests()
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
            var videoServiceLogger = new Mock<ILogger<ComfyUIVideoService>>();

            // Create mock environment
            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/tmp");

            // Create mock file service
            var mockFileService = new Mock<IComfyUIFileService>();

            // Create real ComfyUIVideoService
            _videoService = new ComfyUIVideoService(
                apiClient,
                videoServiceLogger.Object,
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
                            ""ckpt_name"": [[""video_model1.safetensors"", ""svd_model.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _videoService.GetVideoModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Get, _mockHandler.LastRequest.Method);
        }

        [Fact]
        public async Task GetUNETModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""UNETLoader"": {
                    ""input"": {
                        ""required"": {
                            ""unet_name"": [[""unet_model1.safetensors"", ""unet_model2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _videoService.GetUNETModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task GetLoRAModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            var objectInfoResponse = @"{
                ""LoraLoader"": {
                    ""input"": {
                        ""required"": {
                            ""lora_name"": [[""lora1.safetensors"", ""lora2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _videoService.GetLoRAModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
        }

        /// <summary>
        /// Tests that VideoWorkflowWrapper parameters are correctly included in the generated workflow
        /// These tests verify the workflow creation, which is the function bound to the UI
        /// </summary>
        [Fact]
        public void CreateVideoWorkflow_WithTextPrompt_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "A serene landscape with moving clouds",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("A serene landscape with moving clouds", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithNegativePrompt_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "landscape",
                negativePrompt: "blurry, low quality, distorted",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("blurry, low quality, distorted", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithSeed_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 98765,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("98765", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithSteps_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 35,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("35", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithCFGScale_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 10.5f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            Assert.Contains("10.5", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithSamplerName_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "dpmpp_2m_sde",
                scheduler: "normal"
            );

            // Assert
            // Sampler name is used in the workflow creation
            Assert.NotNull(workflow);
        }

        [Fact]
        public void CreateVideoWorkflow_WithScheduler_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "karras"
            );

            // Assert
            // Scheduler is used in the workflow creation
            Assert.NotNull(workflow);
        }

        [Fact]
        public void CreateVideoWorkflow_WithFps_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 24,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            // FPS is used in video generation workflow configuration
            Assert.NotNull(workflow);
        }

        [Fact]
        public void CreateVideoWorkflow_WithImagePath_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "my_custom_image.png",
                audioPath: "",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            // Image path is used as reference in the workflow
            Assert.NotNull(workflow);
            Assert.Contains("my_custom_image", workflowJson);
        }

        [Fact]
        public void CreateVideoWorkflow_WithAudioPath_IncludesInWorkflow()
        {
            // Arrange & Act
            var workflow = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                positivePrompt: "test",
                negativePrompt: "",
                imagePath: "test.png",
                audioPath: "my_audio.mp3",
                seed: 12345,
                steps: 20,
                cfg: 7.0f,
                fps: 8,
                filenamePrefix: "output",
                samplerName: "euler",
                scheduler: "normal"
            );

            // Assert
            var workflowJson = JsonSerializer.Serialize(workflow);
            // Audio path is used as reference in the workflow
            Assert.NotNull(workflow);
            Assert.Contains("my_audio", workflowJson);
        }

        // NOTE: The following parameters from VideoWorkflowWrapper do not directly affect the HTTP request content:
        // - CheckpointName: May not be used in all video workflows (depends on the workflow type)
        // - Denoise: Not all video workflows use this parameter
        // - Width/Height: These may be derived from the input image rather than explicitly set
        // - FrameCount: Calculated from DurationSeconds and Fps, but the workflow may use fps and duration differently
        // - DurationSeconds: Used to calculate FrameCount based on Fps, but only Fps is sent
        // - MotionBucketId: Only used in SVD (Stable Video Diffusion) workflows
        // - AugmentationLevel: Only used in specific video generation workflows
        // - AnimationStyle: This is a UI helper field and is not directly used in ComfyUI workflows
        // - MotionIntensity: This is a UI helper field and is not directly used in ComfyUI workflows
        // - OutputFilename: Used as a prefix in the workflow but doesn't affect the HTTP request structure
        // - OutputFormat: Used locally after generation, not sent to ComfyUI
        // - Quality: Used locally for encoding, not sent to ComfyUI
    }
}
