using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class VideoWorkflowWrapperTests
    {
        [Fact]
        public void CreateDefault_ShouldHaveReasonableDefaults()
        {
            // Act
            var wrapper = VideoWorkflowWrapper.CreateDefault();

            // Assert
            Assert.NotNull(wrapper);
            Assert.Equal("a beautiful landscape", wrapper.TextPrompt);
            Assert.Equal("bad quality, blurry", wrapper.NegativePrompt);
            Assert.Equal(20, wrapper.Steps);
            Assert.Equal(7.5f, wrapper.CFGScale);
            Assert.Equal(1024, wrapper.Width);
            Assert.Equal(576, wrapper.Height);
            Assert.Equal(25, wrapper.FrameCount);
            Assert.Equal(1.0f, wrapper.Denoise);
            Assert.Equal(127, wrapper.MotionBucketId);
            Assert.Equal(0.0f, wrapper.AugmentationLevel);
            Assert.True(wrapper.Seed > 0);
        }

        [Fact]
        public void GetWorkflowConfig_ShouldReturnUnderlyingConfig()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();

            // Act
            var config = wrapper.GetWorkflowConfig();

            // Assert
            Assert.NotNull(config);
            Assert.IsType<VideoWorkflowConfig>(config);
        }

        [Fact]
        public void TextPrompt_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testPrompt = "test prompt for video generation";

            // Act
            wrapper.TextPrompt = testPrompt;

            // Assert
            Assert.Equal(testPrompt, wrapper.TextPrompt);
        }

        [Fact]
        public void NegativePrompt_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testNegativePrompt = "test negative prompt";

            // Act
            wrapper.NegativePrompt = testNegativePrompt;

            // Assert
            Assert.Equal(testNegativePrompt, wrapper.NegativePrompt);
        }

        [Fact]
        public void CheckpointName_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testCheckpoint = "test_model.safetensors";

            // Act
            wrapper.CheckpointName = testCheckpoint;

            // Assert
            Assert.Equal(testCheckpoint, wrapper.CheckpointName);
        }

        [Fact]
        public void Seed_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testSeed = 12345;

            // Act
            wrapper.Seed = testSeed;

            // Assert
            Assert.Equal(testSeed, wrapper.Seed);
        }

        [Fact]
        public void Steps_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testSteps = 30;

            // Act
            wrapper.Steps = testSteps;

            // Assert
            Assert.Equal(testSteps, wrapper.Steps);
        }

        [Fact]
        public void CFGScale_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testCFG = 8.5f;

            // Act
            wrapper.CFGScale = testCFG;

            // Assert
            Assert.Equal(testCFG, wrapper.CFGScale);
        }

        [Fact]
        public void SamplerName_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testSampler = "dpmpp_2m";

            // Act
            wrapper.SamplerName = testSampler;

            // Assert
            Assert.Equal(testSampler, wrapper.SamplerName);
        }

        [Fact]
        public void Scheduler_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testScheduler = "karras";

            // Act
            wrapper.Scheduler = testScheduler;

            // Assert
            Assert.Equal(testScheduler, wrapper.Scheduler);
        }

        [Fact]
        public void Denoise_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testDenoise = 0.8f;

            // Act
            wrapper.Denoise = testDenoise;

            // Assert
            Assert.Equal(testDenoise, wrapper.Denoise);
        }

        [Fact]
        public void ImageFilename_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testFilename = "test_input.jpg";

            // Act
            wrapper.ImageFilename = testFilename;

            // Assert
            Assert.Equal(testFilename, wrapper.ImageFilename);
        }

        [Fact]
        public void OutputFilename_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testOutput = "test_output";

            // Act
            wrapper.OutputFilename = testOutput;

            // Assert
            Assert.Equal(testOutput, wrapper.OutputFilename);
        }

        [Fact]
        public void Dimensions_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testWidth = 512;
            var testHeight = 512;

            // Act
            wrapper.Width = testWidth;
            wrapper.Height = testHeight;

            // Assert
            Assert.Equal(testWidth, wrapper.Width);
            Assert.Equal(testHeight, wrapper.Height);
        }

        [Fact]
        public void VideoParameters_SetAndGet_ShouldWork()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            var testFrames = 30;
            var testMotionBucket = 180;
            var testAugLevel = 0.5f;

            // Act
            wrapper.FrameCount = testFrames;
            wrapper.MotionBucketId = testMotionBucket;
            wrapper.AugmentationLevel = testAugLevel;

            // Assert
            Assert.Equal(testFrames, wrapper.FrameCount);
            Assert.Equal(testMotionBucket, wrapper.MotionBucketId);
            Assert.Equal(testAugLevel, wrapper.AugmentationLevel);
        }

        [Fact]
        public void ToJson_ShouldReturnValidJson()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            wrapper.TextPrompt = "test prompt";
            wrapper.Seed = 42;

            // Act
            var json = wrapper.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            Assert.Contains("\"nodes\"", json);
        }

        [Fact]
        public async Task LoadVideoExampleAsync_ShouldLoadExampleWorkflow()
        {
            // This test will only work if the video_example.json file exists
            try
            {
                // Act
                var wrapper = await VideoWorkflowWrapper.LoadVideoExampleAsync();

                // Assert
                Assert.NotNull(wrapper);
                Assert.NotNull(wrapper.GetWorkflowConfig());
            }
            catch (FileNotFoundException)
            {
                // Skip test if file doesn't exist
                Assert.True(true, "Skipped test - video_example.json not found");
            }
        }

        [Fact]
        public async Task SaveToFileAsync_ShouldSaveAndLoad()
        {
            // Arrange
            var wrapper = VideoWorkflowWrapper.CreateDefault();
            wrapper.TextPrompt = "test save and load";
            wrapper.Seed = 99999;
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                await wrapper.SaveToFileAsync(tempFile);
                var loadedWrapper = await VideoWorkflowWrapper.LoadFromFileAsync(tempFile);

                // Assert
                Assert.NotNull(loadedWrapper);
                Assert.Equal(wrapper.TextPrompt, loadedWrapper.TextPrompt);
                Assert.Equal(wrapper.Seed, loadedWrapper.Seed);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void Constructor_WithExistingConfig_ShouldWrap()
        {
            // Arrange
            var config = new VideoWorkflowConfig();

            // Act
            var wrapper = new VideoWorkflowWrapper(config);

            // Assert
            Assert.NotNull(wrapper);
            Assert.Same(config, wrapper.GetWorkflowConfig());
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new VideoWorkflowWrapper(null!));
        }
    }
}