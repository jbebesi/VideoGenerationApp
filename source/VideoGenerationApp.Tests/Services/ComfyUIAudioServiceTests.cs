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
    public class ComfyUIAudioServiceTests
    {
        private readonly Mock<ILogger<ComfyUIAudioService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<IOptions<ComfyUISettings>> _settingsMock;
        private readonly Mock<IComfyUIApiClient> _apiClientMock;
        private readonly Mock<IComfyUIFileService> _fileServiceMock;
        private readonly ComfyUIAudioService _comfyUIAudioService;
        private readonly ComfyUISettings _settings;

        public ComfyUIAudioServiceTests()
        {
            _loggerMock = new Mock<ILogger<ComfyUIAudioService>>();
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

            _comfyUIAudioService = new ComfyUIAudioService(
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
            var result = _comfyUIAudioService.GetWorkflowConfig();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ace_step_v1_3.5b.safetensors", result.CheckpointName);
            Assert.Equal("pop, female voice, catchy melody", result.Tags);
            Assert.Equal(120f, result.AudioDurationSeconds);
        }

        [Fact]
        public void SetWorkflowConfig_UpdatesConfig_WhenCalled()
        {
            // Arrange
            var newConfig = new AudioWorkflowConfig
            {
                CheckpointName = "custom_model.safetensors",
                Tags = "rock, male voice, aggressive",
                AudioDurationSeconds = 60f,
                Steps = 30
            };

            // Act
            _comfyUIAudioService.SetWorkflowConfig(newConfig);
            var result = _comfyUIAudioService.GetWorkflowConfig();

            // Assert
            Assert.Equal("custom_model.safetensors", result.CheckpointName);
            Assert.Equal("rock, male voice, aggressive", result.Tags);
            Assert.Equal(60f, result.AudioDurationSeconds);
            Assert.Equal(30, result.Steps);
        }

        [Fact]
        public void GetWorkflowTemplate_ReturnsValidJson_WhenCalled()
        {
            // Act
            var result = _comfyUIAudioService.GetWorkflowTemplate();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify it's valid JSON
            var workflow = JsonSerializer.Deserialize<ComfyUIAudioWorkflow>(result);
            Assert.NotNull(workflow);
            Assert.NotEmpty(workflow.nodes);
        }

        [Fact]
        public void SetWorkflowTemplate_UpdatesConfigFromJson_WhenValidJson()
        {
            // Arrange
            var originalConfig = _comfyUIAudioService.GetWorkflowConfig();
            var workflowJson = _comfyUIAudioService.GetWorkflowTemplate();

            // Modify the config to have different values
            var newConfig = new AudioWorkflowConfig
            {
                CheckpointName = "different_model.safetensors",
                Tags = "jazz, saxophone",
                Steps = 25
            };
            _comfyUIAudioService.SetWorkflowConfig(newConfig);

            // Act
            _comfyUIAudioService.SetWorkflowTemplate(workflowJson);

            // Assert
            var resultConfig = _comfyUIAudioService.GetWorkflowConfig();
            // The config should be extracted from the workflow template
            Assert.NotNull(resultConfig);
        }

        [Fact]
        public void SetWorkflowTemplate_KeepsCurrentConfig_WhenInvalidJson()
        {
            // Arrange
            var originalConfig = _comfyUIAudioService.GetWorkflowConfig();
            var originalSteps = originalConfig.Steps;
            var invalidJson = "{ invalid json structure";

            // Act
            _comfyUIAudioService.SetWorkflowTemplate(invalidJson);

            // Assert
            var resultConfig = _comfyUIAudioService.GetWorkflowConfig();
            Assert.Equal(originalSteps, resultConfig.Steps); // Should remain unchanged
        }

        [Fact]
        public async Task GetQueueStatusAsync_ReturnsStatus_WhenSuccessful()
        {
            // Arrange
            var queueResponse = new QueueResponse
            {
                QueuePending = new List<ComfyUI.Client.Models.Responses.QueueItem>(),
                QueueRunning = new List<ComfyUI.Client.Models.Responses.QueueItem>()
            };

            _apiClientMock.Setup(x => x.GetQueueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queueResponse);

            // Act
            var result = await _comfyUIAudioService.GetQueueStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.exec);
            Assert.NotNull(result.queue);
        }

        [Fact]
        public async Task GetQueueStatusAsync_ReturnsNull_WhenApiClientFails()
        {
            // Arrange
            _apiClientMock.Setup(x => x.GetQueueAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            // Act
            var result = await _comfyUIAudioService.GetQueueStatusAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GenerateAudioAsync_CallsGenerateAsync_WhenCalled()
        {
            // Arrange
            var sceneOutput = new VideoSceneOutput
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

            // Mock system stats to simulate ComfyUI running
            _apiClientMock.Setup(x => x.GetSystemStatsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemStatsResponse());

            // Mock successful workflow submission
            _apiClientMock.Setup(x => x.SubmitPromptAsync(It.IsAny<ComfyUI.Client.Models.Requests.PromptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PromptResponse { PromptId = "test-prompt-123" });

            // Mock queue status to simulate completion
            var queueResponse = new QueueResponse
            {
                QueuePending = new List<ComfyUI.Client.Models.Responses.QueueItem>(),
                QueueRunning = new List<ComfyUI.Client.Models.Responses.QueueItem>()
            };
            _apiClientMock.Setup(x => x.GetQueueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queueResponse);

            // Mock history response to simulate completed task with outputs
            var historyResponse = new Dictionary<string, object>
            {
                ["test-prompt-123"] = new Dictionary<string, object>
                {
                    ["outputs"] = new Dictionary<string, object>
                    {
                        ["1"] = new Dictionary<string, object>
                        {
                            ["audio"] = new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    ["filename"] = "test_audio.wav",
                                    ["subfolder"] = "audio"
                                }
                            }
                        }
                    }
                }
            };
            _apiClientMock.Setup(x => x.GetHistoryAsync("test-prompt-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(historyResponse);

            // Mock file download
            _apiClientMock.Setup(x => x.GetImageAsync("test_audio.wav", null, "audio", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 1, 2, 3, 4 }); // Mock audio file bytes

            // Act
            var result = await _comfyUIAudioService.GenerateAudioAsync(sceneOutput);

            // Assert
            // The method should return a file path when successful
            Assert.NotNull(result);
            Assert.StartsWith("/audio/", result);
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_ReturnsValidFormat_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                CheckpointName = "test_model.safetensors",
                Tags = "test tags",
                Lyrics = "test lyrics",
                Steps = 25,
                CFGScale = 7.5f
            };

            var workflow = AudioWorkflowFactory.CreateWorkflow(config);

            // Act
            var result = _comfyUIAudioService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify structure
            foreach (var kvp in result)
            {
                var nodeData = kvp.Value as Dictionary<string, object>;
                Assert.NotNull(nodeData);
                Assert.True(nodeData.ContainsKey("class_type"));
                Assert.True(nodeData.ContainsKey("inputs"));
            }
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_HandlesKSamplerNode_Correctly()
        {
            // Arrange
            var workflow = new ComfyUIAudioWorkflow
            {
                nodes = new List<ComfyUINode>
                {
                    new ComfyUINode
                    {
                        id = 1,
                        type = "KSampler",
                        widgets_values = new object[] { 12345, "fixed", 20, 7.5, "euler", "normal", 1.0 }
                    }
                }
            };

            // Act
            var result = _comfyUIAudioService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            var node = result["1"] as Dictionary<string, object>;
            Assert.NotNull(node);
            var inputs = node["inputs"] as Dictionary<string, object>;
            Assert.NotNull(inputs);
            
            Assert.Equal(12345, inputs["seed"]);
            Assert.Equal(20, inputs["steps"]);
            Assert.Equal(7.5, inputs["cfg"]);
            Assert.Equal("euler", inputs["sampler_name"]);
        }

        [Fact]
        public void ConvertWorkflowToComfyUIFormat_HandlesTextEncodeAceStepAudioNode_Correctly()
        {
            // Arrange
            var workflow = new ComfyUIAudioWorkflow
            {
                nodes = new List<ComfyUINode>
                {
                    new ComfyUINode
                    {
                        id = 2,
                        type = "TextEncodeAceStepAudio",
                        widgets_values = new object[] { "pop, upbeat", "test lyrics", 0.95 }
                    }
                }
            };

            // Act
            var result = _comfyUIAudioService.ConvertWorkflowToComfyUIFormat(workflow);

            // Assert
            var node = result["2"] as Dictionary<string, object>;
            Assert.NotNull(node);
            var inputs = node["inputs"] as Dictionary<string, object>;
            Assert.NotNull(inputs);
            
            Assert.Equal("pop, upbeat", inputs["tags"]);
            Assert.Equal("test lyrics", inputs["lyrics"]);
            Assert.Equal(0.95, inputs["lyrics_strength"]);
        }

        [Fact]
        public async Task GetAudioModelsAsync_ReturnsModels_WhenSuccessful()
        {
            // Arrange
            var expectedModels = new List<string> { "ace_step_v1_3.5b.safetensors", "other_model.safetensors" };
            
            // Mock GetObjectInfoAsync to return a proper structure for CheckpointLoaderSimple
            var objectInfo = new Dictionary<string, object>
            {
                ["CheckpointLoaderSimple"] = new Dictionary<string, object>
                {
                    ["input"] = new Dictionary<string, object>
                    {
                        ["required"] = new Dictionary<string, object>
                        {
                            ["ckpt_name"] = new object[] { expectedModels.ToArray() }
                        }
                    }
                }
            };
            
            _apiClientMock.Setup(x => x.GetObjectInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(objectInfo);

            // Act
            var result = await _comfyUIAudioService.GetAudioModelsAsync();

            // Assert
            // The service filters models to only return ones containing "ace", "audio", or "step"
            Assert.Contains("ace_step_v1_3.5b.safetensors", result);
        }
    }
}