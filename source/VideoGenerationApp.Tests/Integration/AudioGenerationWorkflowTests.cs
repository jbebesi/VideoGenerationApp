using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using Xunit;

namespace VideoGenerationApp.Tests.Integration
{
    public class AudioGenerationWorkflowTests
    {
        private readonly Mock<IComfyUIAudioService> _mockAudioService;
        private readonly Mock<IGenerationQueueService> _mockQueueService;
        private readonly Mock<ILogger<AudioGenerationWorkflow>> _mockLogger;

        public AudioGenerationWorkflowTests()
        {
            _mockAudioService = new Mock<IComfyUIAudioService>();
            _mockQueueService = new Mock<IGenerationQueueService>();
            _mockLogger = new Mock<ILogger<AudioGenerationWorkflow>>();
        }

        [Fact]
        public async Task AudioGenerationWorkflow_GeneratesSuccessfully_WithValidConfig()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            
            var config = new AudioWorkflowConfig
            {
                Tags = "pop, female voice, catchy melody",
                Lyrics = "[verse]\nDancing through the night\n[chorus]\nFeel the music bright",
                AudioDurationSeconds = 180
            };

            _mockQueueService.Setup(x => x.QueueTaskAsync(It.IsAny<GenerationTaskBase>()))
                .ReturnsAsync("test-task-id-123");

            // Act
            var taskId = await workflow.GenerateAsync("Test Audio Generation", config, "Test notes");

            // Assert
            Assert.Equal("test-task-id-123", taskId);
            _mockQueueService.Verify(x => x.QueueTaskAsync(It.Is<GenerationTaskBase>(t => 
                t.Name == "Test Audio Generation" && 
                t.Type == GenerationType.Audio &&
                t.Notes == "Test notes")), Times.Once);
        }

        [Fact]
        public async Task AudioGenerationWorkflow_GetAvailableModels_CallsAudioService()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            var expectedModels = new List<string> { "ace_step_v1_3.5b.safetensors", "other_model.safetensors" };
            
            _mockAudioService.Setup(x => x.GetAudioModelsAsync())
                .ReturnsAsync(expectedModels);

            // Act
            var models = await workflow.GetAvailableModelsAsync();

            // Assert
            Assert.Equal(expectedModels, models);
            _mockAudioService.Verify(x => x.GetAudioModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task AudioGenerationWorkflow_GetCLIPModels_CallsAudioService()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            var expectedModels = new List<string> { "clip-vit-large-patch14.safetensors" };
            
            _mockAudioService.Setup(x => x.GetCLIPModelsAsync())
                .ReturnsAsync(expectedModels);

            // Act
            var models = await workflow.GetCLIPModelsAsync();

            // Assert
            Assert.Equal(expectedModels, models);
            _mockAudioService.Verify(x => x.GetCLIPModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task AudioGenerationWorkflow_GetVAEModels_CallsAudioService()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            var expectedModels = new List<string> { "vae-ft-mse-840000-ema-pruned.safetensors" };
            
            _mockAudioService.Setup(x => x.GetVAEModelsAsync())
                .ReturnsAsync(expectedModels);

            // Act
            var models = await workflow.GetVAEModelsAsync();

            // Assert
            Assert.Equal(expectedModels, models);
            _mockAudioService.Verify(x => x.GetVAEModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task AudioGenerationWorkflow_GetAudioEncoderModels_CallsAudioService()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            var expectedModels = new List<string> { "audio_encoder_v1.safetensors" };
            
            _mockAudioService.Setup(x => x.GetAudioEncoderModelsAsync())
                .ReturnsAsync(expectedModels);

            // Act
            var models = await workflow.GetAudioEncoderModelsAsync();

            // Assert
            Assert.Equal(expectedModels, models);
            _mockAudioService.Verify(x => x.GetAudioEncoderModelsAsync(), Times.Once);
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
        public async Task AudioGenerationWorkflow_HandlesExceptions_Gracefully()
        {
            // Arrange
            var workflow = new AudioGenerationWorkflow(_mockQueueService.Object, _mockAudioService.Object, _mockLogger.Object);
            
            _mockAudioService.Setup(x => x.GetAudioModelsAsync())
                .ThrowsAsync(new Exception("ComfyUI connection failed"));

            // Act
            var models = await workflow.GetAvailableModelsAsync();

            // Assert
            Assert.Empty(models);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting audio models")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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

        [Fact]
        public void ServiceIntegration_WorksCorrectly_WithDependencyInjection()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IOptions<ComfyUISettings>>(provider => 
                Options.Create(new ComfyUISettings { ApiUrl = "http://localhost:8188" }));

            using var serviceProvider = services.BuildServiceProvider();

            // Act
            var comfyUISettings = serviceProvider.GetService<IOptions<ComfyUISettings>>();

            // Assert
            Assert.NotNull(comfyUISettings);
            Assert.Equal("http://localhost:8188", comfyUISettings.Value.ApiUrl);
        }
    }
}