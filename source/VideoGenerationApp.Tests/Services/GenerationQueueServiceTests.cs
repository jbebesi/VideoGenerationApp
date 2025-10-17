using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using Xunit;

namespace VideoGenerationApp.Tests.Services
{
    public class GenerationQueueServiceTests
    {
        private readonly Mock<ILogger<GenerationQueueService>> _loggerMock;
        private readonly GenerationQueueService _generationQueueService;

        /// <summary>
        /// Helper method to create a test wrapper for testing
        /// </summary>
        private static VideoWorkflowWrapper CreateTestWrapper()
        {
            var examplePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "..",
                "Doc", "ComfyUI", "Example_Workflows", "video_example.json");
            
            if (File.Exists(examplePath))
            {
                var json = File.ReadAllText(examplePath);
                var config = JsonSerializer.Deserialize<VideoWorkflowConfig>(json);
                return new VideoWorkflowWrapper(config!);
            }
            
            // Fallback: create an empty config and wrapper
            return new VideoWorkflowWrapper(new VideoWorkflowConfig 
            { 
                nodes = new Node1[0], 
                links = new object[0][],
                groups = new Group[0],
                definitions = new Definitions { subgraphs = new Subgraph[0] },
                config = new Config1(),
                extra = new Extra1(),
                id = "test",
                version = 1.0f
            });
        }

        public GenerationQueueServiceTests()
        {
            _loggerMock = new Mock<ILogger<GenerationQueueService>>();
            
            // Create the simplified queue service with only logger dependency
            _generationQueueService = new GenerationQueueService(_loggerMock.Object);
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
        public async Task QueueTaskAsync_AddsTaskToQueue_WhenCalled()
        {
            // Arrange
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            wrapper.TextPrompt = "Test video generation";
            
            var task = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object)
            {
                Name = "Test Task"
            };

            // Mock successful submission
            mockVideoService.Setup(x => x.SubmitWorkflowAsync(It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync("test-prompt-123");

            // Act
            var taskId = await _generationQueueService.QueueTaskAsync(task);

            // Assert
            Assert.NotNull(taskId);
            Assert.Equal(task.Id, taskId);
            
            var retrievedTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(retrievedTask);
            Assert.Equal("Test Task", retrievedTask.Name);
            Assert.Equal(GenerationType.Video, retrievedTask.Type);
        }

        [Fact]
        public void GetTask_ReturnsTask_WhenTaskExists()
        {
            // Arrange
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var task = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object)
            {
                Name = "Test Task"
            };
            
            var taskId = _generationQueueService.QueueTaskAsync(task).Result;

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
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var task1 = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Task 1" };
            var task2 = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Task 2" };
            var task3 = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Task 3" };
            
            var taskId1 = _generationQueueService.QueueTaskAsync(task1).Result;
            var taskId2 = _generationQueueService.QueueTaskAsync(task2).Result;
            var taskId3 = _generationQueueService.QueueTaskAsync(task3).Result;

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
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var task1 = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Async Task 1" };
            var task2 = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Async Task 2" };
            
            await _generationQueueService.QueueTaskAsync(task1);
            await _generationQueueService.QueueTaskAsync(task2);

            // Act
            var result = (await _generationQueueService.GetAllTasksAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task CancelTaskAsync_CancelsTask_WhenTaskIsPending()
        {
            // Arrange
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var task = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object)
            {
                Name = "Cancellable Task"
            };
            
            var taskId = await _generationQueueService.QueueTaskAsync(task);

            // Act
            var result = await _generationQueueService.CancelTaskAsync(taskId);

            // Assert
            Assert.True(result);
            var cancelledTask = _generationQueueService.GetTask(taskId);
            Assert.NotNull(cancelledTask);
            Assert.Equal(GenerationStatus.Cancelled, cancelledTask.Status);
            Assert.Equal("Cancelled by user", cancelledTask.ErrorMessage);
            Assert.NotNull(cancelledTask.CompletedAt);
        }

        [Fact]
        public async Task CancelTaskAsync_ReturnsFalse_WhenTaskDoesNotExist()
        {
            // Act
            var result = await _generationQueueService.CancelTaskAsync("non-existent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ClearCompletedTasks_RemovesCompletedTasks_WhenCalled()
        {
            // Arrange
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var pendingTask = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Pending Task" };
            var cancelledTask = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object) { Name = "Cancelled Task" };
            
            var pendingTaskId = await _generationQueueService.QueueTaskAsync(pendingTask);
            var cancelledTaskId = await _generationQueueService.QueueTaskAsync(cancelledTask);
            
            // Cancel one task to make it completed
            await _generationQueueService.CancelTaskAsync(cancelledTaskId);

            // Act
            var removedCount = _generationQueueService.ClearCompletedTasks();

            // Assert
            Assert.Equal(1, removedCount); // Should remove the cancelled task
            Assert.NotNull(_generationQueueService.GetTask(pendingTaskId)); // Pending should remain
            Assert.Null(_generationQueueService.GetTask(cancelledTaskId)); // Cancelled should be removed
        }

        [Fact]
        public async Task TaskStatusChanged_EventTriggered_WhenTaskStatusChanges()
        {
            // Arrange
            var mockVideoService = new Mock<IComfyUIVideoService>();
            var mockApiClient = new Mock<ComfyUI.Client.Services.IComfyUIApiClient>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var wrapper = CreateTestWrapper();
            var task = new VideoGenerationTask(wrapper, mockVideoService.Object, mockApiClient.Object, mockEnvironment.Object)
            {
                Name = "Event Test Task"
            };
            
            var taskId = await _generationQueueService.QueueTaskAsync(task);

            GenerationTask? changedTask = null;
            _generationQueueService.TaskStatusChanged += (t) => changedTask = t;

            // Act
            await _generationQueueService.CancelTaskAsync(taskId);

            // Assert
            Assert.NotNull(changedTask);
            Assert.Equal(taskId, changedTask.Id);
            Assert.Equal(GenerationStatus.Cancelled, changedTask.Status);
        }

        [Fact]
        public async Task LegacyMethods_ThrowNotSupportedException_WhenCalled()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                _generationQueueService.QueueGenerationAsync("test", new AudioWorkflowConfig()));
                
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                _generationQueueService.QueueImageGenerationAsync("test", new ImageWorkflowConfig()));
                
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                _generationQueueService.QueueVideoGenerationAsync("test", new VideoWorkflowConfig()));
        }
    }
}