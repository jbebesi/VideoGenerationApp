using Bunit;
using ComfyUI.Client.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VideoGenerationApp.Components.Pages;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using Xunit;

namespace VideoGenerationApp.Tests.Components
{
    public class GenerateVideoComponentTests : Bunit.TestContext
    {
        private readonly Mock<IGenerationQueueService> _queueServiceMock;
        private readonly OllamaOutputState _outputState;
        private readonly Mock<ILogger<GenerateVideo>> _loggerMock;
        private readonly Mock<IGeneratedFileService> _generatedFileServiceMock;

        public GenerateVideoComponentTests()
        {
            _queueServiceMock = new Mock<IGenerationQueueService>();
            
            _outputState = new OllamaOutputState();
            _loggerMock = new Mock<ILogger<GenerateVideo>>();

            // Setup default mock behaviors
            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask>());
            _queueServiceMock.Setup(x => x.GetVideoModelsAsync())
                .ReturnsAsync(new List<string>());

            _generatedFileServiceMock = new Mock<IGeneratedFileService>();
            _generatedFileServiceMock.Setup(x => x.GetVideoFilesAsync())
                .Returns(Task.FromResult(new List<GeneratedFileInfo>()));
            _generatedFileServiceMock.Setup(x => x.GetImageFilesAsync())
                .Returns(Task.FromResult(new List<GeneratedFileInfo>()));
            _generatedFileServiceMock.Setup(x => x.GetAudioFilesAsync())
                .Returns(Task.FromResult(new List<GeneratedFileInfo>()));

            Services.AddSingleton(Mock.Of<IWebHostEnvironment>());
            Services.AddSingleton(Mock.Of<IComfyUIApiClient>());
            Services.AddSingleton(Mock.Of<IComfyUIVideoService>());
            Services.AddSingleton(_generatedFileServiceMock.Object);
            Services.AddScoped<VideoGenerationWorkflow>();
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

        [Fact]
        public void Component_InitializesTextPrompt_WithVisualDescription_WhenAvailable()
        {
            // Arrange
            _outputState.ParsedOutput = new VideoSceneOutput
            {
                narrative = "This is the narrative text",
                visual_description = "A beautiful sunset over the ocean with waves gently rolling",
                tone = "calm",
                emotion = "peaceful"
            };

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert - The textarea should be bound to the visual description
            var textarea = component.Find("textarea#textPrompt");
            Assert.NotNull(textarea);
            // In Blazor, @bind sets the value in the component instance, not in HTML
            // We verify by checking if the visual description appears in the rendered output
            Assert.Contains("A beautiful sunset over the ocean", component.Markup);
        }

        [Fact]
        public void Component_InitializesTextPrompt_WithNarrative_WhenVisualDescriptionMissing()
        {
            // Arrange
            _outputState.ParsedOutput = new VideoSceneOutput
            {
                narrative = "This is the narrative text for fallback",
                visual_description = "", // Empty visual description
                tone = "calm",
                emotion = "peaceful"
            };

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert - The textarea should be bound to the narrative as fallback
            var textarea = component.Find("textarea#textPrompt");
            Assert.NotNull(textarea);
            // Verify the narrative is used as fallback
            Assert.Contains("This is the narrative text for fallback", component.Markup);
        }

        [Fact]
        public void Component_InitializesTextPrompt_AsEmpty_WhenNoOutputStateData()
        {
            // Arrange
            _outputState.ParsedOutput = null;

            // Act
            var component = RenderComponent<GenerateVideo>();

            // Assert - The textarea should exist
            var textarea = component.Find("textarea#textPrompt");
            Assert.NotNull(textarea);
        }

        [Fact]
        public void Component_UpdatesVideoDimensions_WhenImageIsSelected()
        {
            // Arrange
            var imageTask = new GenerationTask
            {
                Id = "image-1",
                Name = "Test Image",
                Type = GenerationType.Image,
                Status = GenerationStatus.Completed,
                GeneratedFilePath = "/images/test.png",
                ImageConfig = new ImageWorkflowConfig 
                { 
                    Width = 512, 
                    Height = 768 
                }
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask> { imageTask });

            var component = RenderComponent<GenerateVideo>();

            // Act - Select the image
            var imageSelect = component.Find("select#imageFile");
            imageSelect.Change("/images/test.png");

            // Assert - The video dimensions should match the image dimensions
            var widthInput = component.Find("input#width");
            var heightInput = component.Find("input#height");
            
            // The bound value should be updated in the component
            Assert.Contains("512", widthInput.GetAttribute("value") ?? "");
            Assert.Contains("768", heightInput.GetAttribute("value") ?? "");
            
            // Helper text should be shown
            Assert.Contains("Matches selected image width", component.Markup);
            Assert.Contains("Matches selected image height", component.Markup);
        }
    }
}
