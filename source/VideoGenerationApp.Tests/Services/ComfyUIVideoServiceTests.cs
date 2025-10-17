using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using ComfyUI.Client.Services;
using ComfyUI.Client.Models.Responses;
using Xunit;

namespace VideoGenerationApp.Tests.Services
{
    public class ComfyUIVideoServiceTests
    {
        private readonly Mock<ILogger<ComfyUIVideoService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<IOptions<ComfyUISettings>> _settingsMock;
        private readonly Mock<IComfyUIApiClient> _apiClientMock;
        private readonly Mock<IComfyUIFileService> _fileServiceMock;
        private readonly ComfyUIVideoService _comfyUIVideoService;
        private readonly ComfyUISettings _settings;

        public ComfyUIVideoServiceTests()
        {
            _loggerMock = new Mock<ILogger<ComfyUIVideoService>>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _apiClientMock = new Mock<IComfyUIApiClient>();
            _fileServiceMock = new Mock<IComfyUIFileService>();
            
            _settings = new ComfyUISettings
            {
                ApiUrl = "http://localhost:8188",
                TimeoutMinutes = 10,
                PollIntervalSeconds = 2
            };
            _settingsMock = new Mock<IOptions<ComfyUISettings>>();
            _settingsMock.Setup(x => x.Value).Returns(_settings);

            _environmentMock.Setup(x => x.WebRootPath).Returns("C:\\TestWebRoot");

            _comfyUIVideoService = new ComfyUIVideoService(
                _apiClientMock.Object, 
                _loggerMock.Object, 
                _environmentMock.Object,
                _settingsMock.Object,
                _fileServiceMock.Object);
        }

        [Fact]
        public void GetWorkflowConfig_ReturnsDefaultConfig_WhenNotSet()
        {
            // Act
            var result = _comfyUIVideoService.GetWorkflowConfig();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("svd_xt.safetensors", result.CheckpointName);
            Assert.Equal(10.0f, result.DurationSeconds);
            Assert.Equal(1024, result.Width);
            Assert.Equal(1024, result.Height);
            Assert.Equal(30, result.Fps);
        }

        [Fact]
        public void SetWorkflowConfig_UpdatesConfig_WhenCalled()
        {
            // Arrange
            var newConfig = new VideoWorkflowConfig
            {
                CheckpointName = "custom_svd_model.safetensors",
                TextPrompt = "A beautiful sunset scene",
                DurationSeconds = 5.0f,
                Width = 512,
                Height = 512,
                Fps = 24,
                MotionIntensity = 0.7f
            };

            // Act
            _comfyUIVideoService.SetWorkflowConfig(newConfig);
            var result = _comfyUIVideoService.GetWorkflowConfig();

            // Assert
            Assert.Equal("custom_svd_model.safetensors", result.CheckpointName);
            Assert.Equal("A beautiful sunset scene", result.TextPrompt);
            Assert.Equal(5.0f, result.DurationSeconds);
            Assert.Equal(512, result.Width);
            Assert.Equal(512, result.Height);
            Assert.Equal(24, result.Fps);
            Assert.Equal(0.7f, result.MotionIntensity);
        }

        [Fact]
        public void GetWorkflowTemplate_ReturnsValidJson_WhenCalled()
        {
            // Act
            var result = _comfyUIVideoService.GetWorkflowTemplate();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify it's valid JSON
            var workflowObject = JsonDocument.Parse(result);
            Assert.NotNull(workflowObject);
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_ReturnsValidDictionary_WithValidWorkflow()
        {
            // Arrange
            var config = new VideoWorkflowConfig();
            var workflow = VideoWorkflowFactory.CreateWorkflow(config);

            // Act
            var result = _comfyUIVideoService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify all nodes are present
            Assert.True(result.Count >= 6, "Should have at least 6 nodes");
            
            // Check that each node has class_type and inputs
            foreach (var kvp in result)
            {
                var nodeData = kvp.Value as Dictionary<string, object>;
                Assert.NotNull(nodeData);
                Assert.True(nodeData.ContainsKey("class_type"));
                Assert.True(nodeData.ContainsKey("inputs"));
            }
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_IncludesImageOnlyCheckpointLoader_WithCorrectParams()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                CheckpointName = "test_model.safetensors"
            };
            var workflow = VideoWorkflowFactory.CreateWorkflow(config);

            // Act
            var result = _comfyUIVideoService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            var checkpointNode = result.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(n => n["class_type"].ToString() == "ImageOnlyCheckpointLoader");
            
            Assert.NotNull(checkpointNode);
            var inputs = checkpointNode["inputs"] as Dictionary<string, object>;
            Assert.NotNull(inputs);
            Assert.Equal("test_model.safetensors", inputs["ckpt_name"]);
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_IncludesSVDConditioning_WithCorrectParams()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                Width = 512,
                Height = 512,
                DurationSeconds = 5.0f,
                Fps = 24,
                MotionIntensity = 0.5f,
                CFGScale = 3.0f,
                Seed = 12345
            };
            var workflow = VideoWorkflowFactory.CreateWorkflow(config);

            // Act
            var result = _comfyUIVideoService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            var svdNode = result.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(n => n["class_type"].ToString() == "SVD_img2vid_Conditioning");
            
            Assert.NotNull(svdNode);
            var inputs = svdNode["inputs"] as Dictionary<string, object>;
            Assert.NotNull(inputs);
            Assert.Equal(512, inputs["width"]);
            Assert.Equal(512, inputs["height"]);
            Assert.Equal(120, inputs["video_frames"]); // 5.0 * 24 = 120
            Assert.Equal(12345L, inputs["seed"]);
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_IncludesVideoSave_WithCorrectParams()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                Fps = 30,
                OutputFilename = "test_video",
                OutputFormat = "mp4",
                Quality = 90
            };
            var workflow = VideoWorkflowFactory.CreateWorkflow(config);

            // Act
            var result = _comfyUIVideoService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            var videoSaveNode = result.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(n => n["class_type"].ToString() == "SaveImage");
            
            Assert.NotNull(videoSaveNode);
            var inputs = videoSaveNode["inputs"] as Dictionary<string, object>;
            Assert.NotNull(inputs);
            Assert.Equal("test_video", inputs["filename_prefix"]);
        }

        [Fact]
        public async Task GenerateAsync_CreatesVideoConfig_FromSceneOutput()
        {
            // Arrange
            var sceneOutput = new VideoSceneOutput
            {
                narrative = "A peaceful mountain landscape",
                visual_description = "Snow-capped peaks with gentle clouds",
                tone = "calm",
                emotion = "peaceful"
            };

            // Mock successful workflow submission
            _apiClientMock.Setup(x => x.SubmitPromptAsync(It.IsAny<ComfyUI.Client.Models.Requests.PromptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PromptResponse { PromptId = "test-prompt-123" });

            // Mock system stats to simulate ComfyUI running
            _apiClientMock.Setup(x => x.GetSystemStatsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemStatsResponse());

            // Act
            var result = await _comfyUIVideoService.GenerateAsync(sceneOutput);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-prompt-123", result);
        }

        [Fact]
        public async Task GenerateVideoAsync_ReturnsPromptId_WhenSuccessful()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                TextPrompt = "Test video generation"
            };

            _apiClientMock.Setup(x => x.SubmitPromptAsync(It.IsAny<ComfyUI.Client.Models.Requests.PromptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PromptResponse { PromptId = "video-prompt-456" });

            // Mock system stats to simulate ComfyUI running
            _apiClientMock.Setup(x => x.GetSystemStatsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemStatsResponse());

            // Act
            var result = await _comfyUIVideoService.GenerateVideoAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("video-prompt-456", result);
        }

        [Fact]
        public async Task GenerateVideoAsync_ReturnsNull_WhenSubmissionFails()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                TextPrompt = "Test video generation"
            };

            _apiClientMock.Setup(x => x.SubmitPromptAsync(It.IsAny<ComfyUI.Client.Models.Requests.PromptRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Submission failed"));

            // Act
            var result = await _comfyUIVideoService.GenerateVideoAsync(config);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SetWorkflowTemplate_DoesNotThrowException_WithValidJson()
        {
            // Arrange
            var workflow = VideoWorkflowFactory.CreateWorkflow(new VideoWorkflowConfig
            {
                CheckpointName = "template_model.safetensors",
                Width = 768,
                Height = 768,
                Fps = 25
            });
            var template = JsonSerializer.Serialize(workflow);

            // Act & Assert - should not throw
            _comfyUIVideoService.SetWorkflowTemplate(template);
            var config = _comfyUIVideoService.GetWorkflowConfig();
            Assert.NotNull(config);
        }

        [Fact]
        public async Task GetVideoModelsAsync_ReturnsModels_WhenSuccessful()
        {
            // Arrange
            var expectedModels = new List<string> { "svd_xt.safetensors", "svd_xt_1_1.safetensors" };
            _apiClientMock.Setup(x => x.GetModelsAsync("checkpoints", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedModels);

            // Act
            var result = await _comfyUIVideoService.GetVideoModelsAsync();

            // Assert
            Assert.Equal(expectedModels, result);
        }
    }
}
