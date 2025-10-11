using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class VideoWorkflowConfigTests
    {
        [Fact]
        public void VideoWorkflowConfig_HasDefaultValues_WhenCreated()
        {
            // Act
            var config = new VideoWorkflowConfig();

            // Assert
            Assert.NotNull(config.TextPrompt);
            Assert.Equal(10.0f, config.DurationSeconds);
            Assert.Equal(1024, config.Width);
            Assert.Equal(1024, config.Height);
            Assert.Equal(30, config.Fps);
            Assert.Equal("smooth", config.AnimationStyle);
            Assert.Equal(0.5f, config.MotionIntensity);
            Assert.Equal(-1, config.Seed);
            Assert.Equal(20, config.Steps);
            Assert.Equal(7.0f, config.CFGScale);
            Assert.Equal(90, config.Quality);
        }

        [Fact]
        public void VideoWorkflowConfig_AllowsCustomization_WhenSet()
        {
            // Act
            var config = new VideoWorkflowConfig
            {
                TextPrompt = "custom video prompt",
                AudioFilePath = "/audio/test.mp3",
                ImageFilePath = "/images/test.png",
                DurationSeconds = 30.0f,
                Width = 1920,
                Height = 1080,
                Fps = 60,
                AnimationStyle = "dynamic",
                MotionIntensity = 0.8f,
                Seed = 54321,
                Steps = 25,
                CFGScale = 8.0f,
                Quality = 95
            };

            // Assert
            Assert.Equal("custom video prompt", config.TextPrompt);
            Assert.Equal("/audio/test.mp3", config.AudioFilePath);
            Assert.Equal("/images/test.png", config.ImageFilePath);
            Assert.Equal(30.0f, config.DurationSeconds);
            Assert.Equal(1920, config.Width);
            Assert.Equal(1080, config.Height);
            Assert.Equal(60, config.Fps);
            Assert.Equal("dynamic", config.AnimationStyle);
            Assert.Equal(0.8f, config.MotionIntensity);
            Assert.Equal(54321, config.Seed);
            Assert.Equal(25, config.Steps);
            Assert.Equal(8.0f, config.CFGScale);
            Assert.Equal(95, config.Quality);
        }

        [Fact]
        public void VideoWorkflowConfig_SupportsOptionalInputs_WhenNotSet()
        {
            // Act
            var config = new VideoWorkflowConfig();

            // Assert
            Assert.Null(config.AudioFilePath);
            Assert.Null(config.ImageFilePath);
        }

        [Fact]
        public void VideoWorkflowConfig_HasSameDefaultDimensions_AsImageWorkflowConfig()
        {
            // Arrange & Act
            var videoConfig = new VideoWorkflowConfig();
            var imageConfig = new ImageWorkflowConfig();

            // Assert - Video and Image should have the same default dimensions
            Assert.Equal(imageConfig.Width, videoConfig.Width);
            Assert.Equal(imageConfig.Height, videoConfig.Height);
        }
    }
}
