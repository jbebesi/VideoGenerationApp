using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using VideoGenerationApp.Components.Pages;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Components
{
    public class GenerateImageComponentTests : Bunit.TestContext
    {
        private readonly Mock<GenerationQueueService> _queueServiceMock;
        private readonly Mock<OllamaOutputState> _outputStateMock;
        private readonly Mock<ILogger<GenerateImage>> _loggerMock;

        public GenerateImageComponentTests()
        {
            _queueServiceMock = new Mock<GenerationQueueService>(Mock.Of<IServiceScopeFactory>(), Mock.Of<ILogger<GenerationQueueService>>());
            _outputStateMock = new Mock<OllamaOutputState>();
            _loggerMock = new Mock<ILogger<GenerateImage>>();

            // Register services
            Services.AddSingleton(_queueServiceMock.Object);
            Services.AddSingleton(_outputStateMock.Object);
            Services.AddSingleton(_loggerMock.Object);
            Services.AddSingleton(Mock.Of<IJSRuntime>());
        }

        [Fact]
        public async Task Component_RendersCorrectly_WhenInitialized()
        {
            // Arrange
            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(new List<GenerationTask>());

            // Act
            var component = RenderComponent<GenerateImage>();

            // Assert
            Assert.Contains("Generate Image", component.Markup);
        }

        [Fact]
        public async Task Component_DisplaysLastGeneratedImage_WhenAvailable()
        {
            // Arrange
            var tasks = new List<GenerationTask>
            {
                new GenerationTask
                {
                    Id = "image-1",
                    Name = "Test Image 1",
                    Type = GenerationType.Image,
                    Status = GenerationStatus.Completed,
                    GeneratedFilePath = "/images/test-image-1.png",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-15)
                },
                new GenerationTask
                {
                    Id = "image-2",
                    Name = "Test Image 2",
                    Type = GenerationType.Image,
                    Status = GenerationStatus.Completed,
                    GeneratedFilePath = "/images/test-image-2.png",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(tasks);

            // Act
            var component = RenderComponent<GenerateImage>();
            await Task.Delay(100); // Allow async initialization to complete

            // Assert
            // Should display the most recent image (test-image-2.png)
            Assert.Contains("/images/test-image-2.png", component.Markup);
            Assert.Contains("Last Generated Image", component.Markup);
        }

        [Fact]
        public async Task Component_ShowsPlaceholder_WhenNoImagesGenerated()
        {
            // Arrange
            var tasks = new List<GenerationTask>
            {
                new GenerationTask
                {
                    Id = "audio-1",
                    Name = "Test Audio",
                    Type = GenerationType.Audio,
                    Status = GenerationStatus.Completed,
                    GeneratedFilePath = "/audio/test-audio.wav",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(tasks);

            // Act
            var component = RenderComponent<GenerateImage>();
            await Task.Delay(100); // Allow async initialization to complete

            // Assert
            // Should show placeholder text when no images are available
            Assert.Contains("Generated image will appear here after generation completes", component.Markup);
        }

        [Fact]
        public async Task Component_IgnoresPendingImages_OnlyShowsCompleted()
        {
            // Arrange
            var tasks = new List<GenerationTask>
            {
                new GenerationTask
                {
                    Id = "pending-image",
                    Name = "Pending Image",
                    Type = GenerationType.Image,
                    Status = GenerationStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new GenerationTask
                {
                    Id = "completed-image",
                    Name = "Completed Image",
                    Type = GenerationType.Image,
                    Status = GenerationStatus.Completed,
                    GeneratedFilePath = "/images/completed-image.png",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    CompletedAt = DateTime.UtcNow.AddMinutes(-8)
                }
            };

            _queueServiceMock.Setup(x => x.GetAllTasksAsync())
                .ReturnsAsync(tasks);

            // Act
            var component = RenderComponent<GenerateImage>();
            await Task.Delay(100); // Allow async initialization to complete

            // Assert
            // Should only show the completed image
            Assert.Contains("/images/completed-image.png", component.Markup);
        }
    }
}
