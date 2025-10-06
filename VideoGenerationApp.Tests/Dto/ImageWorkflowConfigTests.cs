using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class ImageWorkflowConfigTests
    {
        [Fact]
        public void ImageWorkflowConfig_HasDefaultValues_WhenCreated()
        {
            // Act
            var config = new ImageWorkflowConfig();

            // Assert
            Assert.NotNull(config.PositivePrompt);
            Assert.NotNull(config.NegativePrompt);
            Assert.NotNull(config.CheckpointName);
            Assert.Equal(1024, config.Width);
            Assert.Equal(1024, config.Height);
            Assert.Equal(-1, config.Seed);
            Assert.Equal(20, config.Steps);
            Assert.Equal(7.0f, config.CFGScale);
            Assert.Equal("euler", config.SamplerName);
            Assert.Equal("normal", config.Scheduler);
            Assert.Equal(1, config.BatchSize);
        }

        [Fact]
        public void ImageWorkflowConfig_AllowsCustomization_WhenSet()
        {
            // Act
            var config = new ImageWorkflowConfig
            {
                PositivePrompt = "custom positive prompt",
                NegativePrompt = "custom negative prompt",
                Width = 512,
                Height = 768,
                Seed = 12345,
                Steps = 30,
                CFGScale = 10.0f,
                SamplerName = "dpmpp_2m",
                Scheduler = "karras"
            };

            // Assert
            Assert.Equal("custom positive prompt", config.PositivePrompt);
            Assert.Equal("custom negative prompt", config.NegativePrompt);
            Assert.Equal(512, config.Width);
            Assert.Equal(768, config.Height);
            Assert.Equal(12345, config.Seed);
            Assert.Equal(30, config.Steps);
            Assert.Equal(10.0f, config.CFGScale);
            Assert.Equal("dpmpp_2m", config.SamplerName);
            Assert.Equal("karras", config.Scheduler);
        }
    }
}
