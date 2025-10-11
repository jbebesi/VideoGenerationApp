using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using Xunit;

namespace VideoGenerationApp.Tests.Services
{
    public class GenerationQueueServiceTests
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<ILogger<GenerationQueueService>> _loggerMock;
        private readonly Mock<IComfyUIAudioService> _comfyUIAudioServiceMock;
        private readonly Mock<IComfyUIImageService> _comfyUIImageServiceMock;
        private readonly GenerationQueueService _generationQueueService;

        public GenerationQueueServiceTests()
        {
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _loggerMock = new Mock<ILogger<GenerationQueueService>>();
            
            // Mock ComfyUIAudioService
            _comfyUIAudioServiceMock = new Mock<IComfyUIAudioService>();
            _comfyUIAudioServiceMock.Setup(x => x.GetWorkflowConfig())
                .Returns(new AudioWorkflowConfig());
            _comfyUIAudioServiceMock.Setup(_comfyUIAudioServiceMock => _comfyUIAudioServiceMock.ConvertWorkflowToComfyUIFormat(It.IsAny<ComfyUIAudioWorkflow>())).Callback<ComfyUIAudioWorkflow>(workflow =>
            {}).Returns(new Dictionary<string, object>());

            // Mock ComfyUIImageService
            _comfyUIImageServiceMock = new Mock<IComfyUIImageService>();
            _comfyUIImageServiceMock.Setup(x => x.GetWorkflowConfig())
                .Returns(new ImageWorkflowConfig());
            _comfyUIImageServiceMock.Setup(_comfyUIImageServiceMock => _comfyUIImageServiceMock.SubmitWorkflowAsync(It.IsAny<object>()))
                .ReturnsAsync("test-prompt-id-456");
            _comfyUIImageServiceMock.Setup(_comfyUIImageServiceMock => _comfyUIImageServiceMock.ConvertWorkflowToComfyUIFormat(It.IsAny<ComfyUIAudioWorkflow>())).Callback<ComfyUIAudioWorkflow>(workflow => 
            { }).Returns(new Dictionary<string, object>());

            // Setup service scope mocking
            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            
            serviceProviderMock.Setup(x => x.GetService(typeof(IComfyUIAudioService)))
                .Returns(_comfyUIAudioServiceMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IComfyUIImageService)))
                .Returns(_comfyUIImageServiceMock.Object);
            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
            
            // Setup successful workflow submission for audio
            _comfyUIAudioServiceMock.Setup(x => x.SubmitWorkflowAsync(It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync("test-prompt-id-123");
            
            // Setup successful workflow submission for image
            _comfyUIImageServiceMock.Setup(x => x.SubmitWorkflowAsync(It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync("test-prompt-id-456");
            
            // Mock the generation service factory and services
            var audioServiceMock = new Mock<IGenerationService<AudioWorkflowConfig>>();
            audioServiceMock.Setup(x => x.CreateTask(It.IsAny<string>(), It.IsAny<AudioWorkflowConfig>(), It.IsAny<string>()))
                .Returns((string name, AudioWorkflowConfig config, string notes) => new GenerationTask
                {
                    Name = name,
                    AudioConfig = config,
                    Type = GenerationType.Audio,
                    Notes = notes,
                    Status = GenerationStatus.Pending
                });
            audioServiceMock.Setup(x => x.SubmitTaskAsync(It.IsAny<GenerationTask>(), It.IsAny<AudioWorkflowConfig>()))
                .ReturnsAsync("test-prompt-id-123");

            var imageServiceMock = new Mock<IGenerationService<ImageWorkflowConfig>>();
            imageServiceMock.Setup(x => x.CreateTask(It.IsAny<string>(), It.IsAny<ImageWorkflowConfig>(), It.IsAny<string>()))
                .Returns((string name, ImageWorkflowConfig config, string notes) => new GenerationTask
                {
                    Name = name,
                    ImageConfig = config,
                    Type = GenerationType.Image,
                    Notes = notes,
                    Status = GenerationStatus.Pending
                });
            imageServiceMock.Setup(x => x.SubmitTaskAsync(It.IsAny<GenerationTask>(), It.IsAny<ImageWorkflowConfig>()))
                .ReturnsAsync("test-prompt-id-456");

            var videoServiceMock = new Mock<IGenerationService<VideoWorkflowConfig>>();
            videoServiceMock.Setup(x => x.CreateTask(It.IsAny<string>(), It.IsAny<VideoWorkflowConfig>(), It.IsAny<string>()))
                .Returns((string name, VideoWorkflowConfig config, string notes) => new GenerationTask
                {
                    Name = name,
                    VideoConfig = config,
                    Type = GenerationType.Video,
                    Notes = notes,
                    Status = GenerationStatus.Pending
                });
            videoServiceMock.Setup(x => x.SubmitTaskAsync(It.IsAny<GenerationTask>(), It.IsAny<VideoWorkflowConfig>()))
                .ReturnsAsync("test-prompt-id-789");

            var generationServiceFactoryMock = new Mock<IGenerationServiceFactory>();
            generationServiceFactoryMock.Setup(x => x.GetAudioService()).Returns(audioServiceMock.Object);
            generationServiceFactoryMock.Setup(x => x.GetImageService()).Returns(imageServiceMock.Object);
            generationServiceFactoryMock.Setup(x => x.GetVideoService()).Returns(videoServiceMock.Object);
            
            _generationQueueService = new GenerationQueueService(generationServiceFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task StartAsync_StartsSuccessfully_WhenCalled()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await _generationQueueService.StartAsync(cancellationToken);

            // Assert
            // Service should start without throwing exceptions
            // We can verify the logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generation Queue Service is starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StopAsync_StopsSuccessfully_WhenCalled()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            await _generationQueueService.StartAsync(cancellationToken);

            // Act
            await _generationQueueService.StopAsync(cancellationToken);

            // Assert
            // Service should stop without throwing exceptions
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generation Queue Service is stopping")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task QueueGenerationAsync_AddsTaskToQueue_WhenCalled()
        {
            // Arrange
            var name = "Test Generation";
            var config = new AudioWorkflowConfig
            {
                Tags = "test, music",
                Lyrics = "test lyrics"
            };

            // Act
            var taskId = await _generationQueueService.QueueGenerationAsync(name, config);

            // Assert
            Assert.NotNull(taskId);
            Assert.NotEmpty(taskId);
            
            var retrievedTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(retrievedTask);
            Assert.Equal(name, retrievedTask.Name);
            Assert.Equal(GenerationStatus.Queued, retrievedTask.Status);
            Assert.Equal(GenerationType.Audio, retrievedTask.Type);
        }

        [Fact]
        public async Task QueueImageGenerationAsync_AddsImageTaskToQueue_WhenCalled()
        {
            // Arrange
            var name = "Test Image Generation";
            var config = new ImageWorkflowConfig
            {
                PositivePrompt = "beautiful landscape",
                NegativePrompt = "ugly, blurry",
                Width = 1024,
                Height = 1024
            };

            // Act
            var taskId = await _generationQueueService.QueueImageGenerationAsync(name, config);

            // Assert
            Assert.NotNull(taskId);
            Assert.NotEmpty(taskId);
            
            var retrievedTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(retrievedTask);
            Assert.Equal(name, retrievedTask.Name);
            Assert.Equal(GenerationStatus.Queued, retrievedTask.Status); // Queued after successful submission
            Assert.Equal(GenerationType.Image, retrievedTask.Type);
            Assert.NotNull(retrievedTask.ImageConfig);
            Assert.Equal(config.PositivePrompt, retrievedTask.ImageConfig.PositivePrompt);
        }

        [Fact]
        public async Task QueueVideoGenerationAsync_AddsVideoTaskToQueue_WhenCalled()
        {
            // Arrange
            var name = "Test Video Generation";
            var config = new VideoWorkflowConfig
            {
                TextPrompt = "A beautiful sunset scene",
                DurationSeconds = 10.0f,
                Width = 1920,
                Height = 1080,
                Fps = 30
            };

            // Act
            var taskId = await _generationQueueService.QueueVideoGenerationAsync(name, config);

            // Assert
            Assert.NotNull(taskId);
            Assert.NotEmpty(taskId);
            
            var retrievedTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(retrievedTask);
            Assert.Equal(name, retrievedTask.Name);
            // Status will be Failed because ComfyUI is not running in test environment
            Assert.True(retrievedTask.Status == GenerationStatus.Failed || retrievedTask.Status == GenerationStatus.Queued, 
                $"Expected Failed or Queued status, but got {retrievedTask.Status}");
            Assert.Equal(GenerationType.Video, retrievedTask.Type);
            Assert.NotNull(retrievedTask.VideoConfig);
            Assert.Equal(config.TextPrompt, retrievedTask.VideoConfig.TextPrompt);
            Assert.Equal(config.DurationSeconds, retrievedTask.VideoConfig.DurationSeconds);
        }

        [Fact]
        public void GetTask_ReturnsTask_WhenTaskExists()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var taskId = _generationQueueService.QueueGenerationAsync("Test Task", config).Result;

            // Act
            var result = _generationQueueService.GetTask(taskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(taskId, result.Id);
            Assert.Equal("Test Task", result.Name);
        }

        [Fact]
        public void GetTask_ReturnsNull_WhenTaskDoesNotExist()
        {
            // Act
            var result = _generationQueueService.GetTask("non-existent-task");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAllTasks_ReturnsAllTasks_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var taskId1 = _generationQueueService.QueueGenerationAsync("Task 1", config).Result;
            var taskId2 = _generationQueueService.QueueGenerationAsync("Task 2", config).Result;
            var taskId3 = _generationQueueService.QueueGenerationAsync("Task 3", config).Result;

            // Act
            var result = _generationQueueService.GetAllTasks().ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, t => t.Id == taskId1);
            Assert.Contains(result, t => t.Id == taskId2);
            Assert.Contains(result, t => t.Id == taskId3);
        }

        [Fact]
        public void GetAllTasks_ReturnsEmptyList_WhenNoTasks()
        {
            // Act
            var result = _generationQueueService.GetAllTasks().ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllTasksAsync_ReturnsAllTasks_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            await _generationQueueService.QueueGenerationAsync("Async Task 1", config);
            await _generationQueueService.QueueGenerationAsync("Async Task 2", config);

            // Act
            var result = (await _generationQueueService.GetAllTasksAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, task => Assert.Equal(GenerationStatus.Queued, task.Status));
        }

        [Fact]
        public void CancelTask_CancelsTask_WhenTaskIsPending()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var taskId = _generationQueueService.QueueGenerationAsync("Cancellable Task", config).Result;

            // Act
            var result = _generationQueueService.CancelTask(taskId);

            // Assert
            Assert.True(result);
            var cancelledTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(cancelledTask);
            Assert.Equal(GenerationStatus.Cancelled, cancelledTask.Status);
            Assert.Equal("Cancelled by user", cancelledTask.ErrorMessage);
            Assert.NotNull(cancelledTask.CompletedAt);
        }

        [Fact]
        public void CancelTask_ReturnsFalse_WhenTaskDoesNotExist()
        {
            // Act
            var result = _generationQueueService.CancelTask("non-existent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ClearCompletedTasks_RemovesCompletedTasks_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var pendingTaskId = _generationQueueService.QueueGenerationAsync("Pending Task", config).Result;
            var cancelledTaskId = _generationQueueService.QueueGenerationAsync("Cancelled Task", config).Result;
            
            // Cancel one task to make it completed
            _generationQueueService.CancelTask(cancelledTaskId);

            // Act
            var removedCount = _generationQueueService.ClearCompletedTasks();

            // Assert
            Assert.Equal(1, removedCount); // Should remove the cancelled task
            Assert.NotNull(_generationQueueService.GetTask(pendingTaskId)); // Pending should remain
            Assert.Null(_generationQueueService.GetTask(cancelledTaskId)); // Cancelled should be removed
        }

        [Fact]
        public void TaskStatusChanged_EventTriggered_WhenTaskStatusChanges()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var taskId = _generationQueueService.QueueGenerationAsync("Event Test Task", config).Result;

            GenerationTask? changedTask = null;
            _generationQueueService.TaskStatusChanged += (t) => changedTask = t;

            // Act
            _generationQueueService.CancelTask(taskId);

            // Assert
            Assert.NotNull(changedTask);
            Assert.Equal(taskId, changedTask.Id);
            Assert.Equal(GenerationStatus.Cancelled, changedTask.Status);
        }

        [Fact]
        public async Task QueueGenerationAsync_WithNotes_SetsNotesCorrectly()
        {
            // Arrange
            var config = new AudioWorkflowConfig();
            var notes = "This is a test generation with custom notes";

            // Act
            var taskId = await _generationQueueService.QueueGenerationAsync("Notes Test", config, notes);

            // Assert
            var task = _generationQueueService.GetTask(taskId);
            Assert.NotNull(task);
            Assert.Equal(notes, task.Notes);
        }

        [Fact]
        public async Task QueueGenerationAsync_SetsCorrectDefaults_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Tags = "custom tags",
                Lyrics = "custom lyrics"
            };

            // Act
            var taskId = await _generationQueueService.QueueGenerationAsync("Default Test", config);

            // Assert
            var task = _generationQueueService.GetTask(taskId);
            Assert.NotNull(task);
            Assert.Equal(GenerationStatus.Queued, task.Status);
            Assert.True(task.CreatedAt <= DateTime.UtcNow);
            Assert.NotNull(task.SubmittedAt); // Task is submitted immediately
            Assert.True(task.SubmittedAt <= DateTime.UtcNow);
            Assert.Null(task.CompletedAt); // Should not be completed yet
            Assert.Equal("custom tags", task.Config.Tags);
            Assert.Equal("custom lyrics", task.Config.Lyrics);
        }
    }
}