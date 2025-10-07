using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VideoGenerationApp.Components.Pages;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Components
{
    public class GenerateVideoComponentTests : Bunit.TestContext
    {
        private readonly Mock<GenerationQueueService> _queueServiceMock;
        private readonly OllamaOutputState _outputState;
        private readonly Mock<ILogger<GenerateVideo>> _loggerMock;

        public GenerateVideoComponentTests()
        {
            _queueServiceMock = new Mock<GenerationQueueService>(
                MockBehavior.Loose,
                Mock.Of<IServiceScopeFactory>(),
                Mock.Of<ILogger<GenerationQueueService>>());
            
            _outputState = new OllamaOutputState();
            _loggerMock = new Mock<ILogger<GenerateVideo>>();

            // Setup default mock behaviors
            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask>());

            Services.AddSingleton(_queueServiceMock.Object);
            Services.AddSingleton(_outputState);
            Services.AddSingleton(_loggerMock.Object);
            
            // Add JSRuntime mock
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Component_Renders_Successfully()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.NotNull(component);
            Assert.Contains("Generate Video", component.Markup);
        }

        [Fact]
        public void Component_HasVideoConfigurationSection_WhenRendered()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Video Generation Configuration", component.Markup);
            Assert.Contains("Text Prompt", component.Markup);
            Assert.Contains("Duration", component.Markup);
            Assert.Contains("Width", component.Markup);
            Assert.Contains("Height", component.Markup);
            Assert.Contains("FPS", component.Markup);
        }

        [Fact]
        public void Component_HasGenerateButton_WhenRendered()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Generate Video", component.Markup);
            // Check for button element
            var buttons = component.FindAll("button");
            Assert.NotEmpty(buttons);
        }

        [Fact]
        public void Component_DisplaysAudioSelection_WhenAudioTasksExist()
        {
            // Arrange
            var audioTask = new GenerationTask
            {
                Id = "audio-1",
                Name = "Test Audio",
                Type = GenerationType.Audio,
                Status = GenerationStatus.Completed,
                GeneratedFilePath = "/audio/test.wav",
                AudioConfig = new AudioWorkflowConfig { AudioDurationSeconds = 30f }
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask> { audioTask });

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Audio File", component.Markup);
            Assert.Contains("Test Audio", component.Markup);
        }

        [Fact]
        public void Component_DisplaysImageSelection_WhenImageTasksExist()
        {
            // Arrange
            var imageTask = new GenerationTask
            {
                Id = "image-1",
                Name = "Test Image",
                Type = GenerationType.Image,
                Status = GenerationStatus.Completed,
                GeneratedFilePath = "/images/test.png"
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask> { imageTask });

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Base Image", component.Markup);
            Assert.Contains("Test Image", component.Markup);
        }

        [Fact]
        public async Task Component_QueuesVideoGeneration_WhenGenerateButtonClicked()
        {
            // Arrange
            var taskId = "test-video-task-id";
            _queueServiceMock.Setup(x => x.QueueVideoGenerationAsync(
                    It.IsAny<string>(),
                    It.IsAny<VideoWorkflowConfig>(),
                    It.IsAny<string>()))
                .ReturnsAsync(taskId);

            var component = RenderComponent<GenerateVideo>();

            // Find the generate button
            var buttons = component.FindAll("button");
            var generateButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Generate Video"));
            Assert.NotNull(generateButton);

            // Act
            await generateButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // Assert
            _queueServiceMock.Verify(x => x.QueueVideoGenerationAsync(
                It.IsAny<string>(),
                It.IsAny<VideoWorkflowConfig>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Component_ShowsOllamaSceneReference_WhenOutputStateHasData()
        {
            // Arrange
            _outputState.ParsedOutput = new VideoSceneOutput
            {
                narrative = "Test narrative",
                visual_description = "Test visual description",
                tone = "calm",
                emotion = "peaceful"
            };

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Ollama Scene Reference", component.Markup);
            Assert.Contains("Test narrative", component.Markup);
            Assert.Contains("Test visual description", component.Markup);
        }

        [Fact]
        public void Component_HasAnimationStyleOptions_WhenRendered()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Animation Style", component.Markup);
            Assert.Contains("static", component.Markup);
            Assert.Contains("smooth", component.Markup);
            Assert.Contains("dynamic", component.Markup);
        }

        [Fact]
        public void Component_HasMotionIntensityInput_WhenRendered()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Motion Intensity", component.Markup);
        }

        [Fact]
        public void Component_HasQualitySettings_WhenRendered()
        {
            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert
            Assert.Contains("Quality", component.Markup);
            Assert.Contains("Generation Steps", component.Markup);
            Assert.Contains("CFG Scale", component.Markup);
        }
    }
}
