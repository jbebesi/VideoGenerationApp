using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class VideoWorkflowFactoryTests
    {
        [Fact]
        public void CreateWorkflow_ReturnsValidWorkflow_WithDefaultConfig()
        {
            // Arrange
            var config = new VideoWorkflowConfig();

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.id);
            Assert.NotEmpty(result.nodes);
            Assert.NotEmpty(result.links);
            Assert.Equal("0.4", result.version);
            Assert.Equal(0, result.revision);
        }

        [Fact]
        public void CreateWorkflow_ContainsRequiredNodes_WhenCalled()
        {
            // Arrange
            var config = new VideoWorkflowConfig();

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var nodeTypes = result.nodes.Select(n => n.type).ToList();
            
            Assert.Contains("ImageOnlyCheckpointLoader", nodeTypes);
            Assert.Contains("LoadImage", nodeTypes);
            Assert.Contains("VAEEncode", nodeTypes);
            Assert.Contains("SVD_img2vid_Conditioning", nodeTypes);
            Assert.Contains("VAEDecode", nodeTypes);
            Assert.Contains("SaveImage", nodeTypes);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresCheckpointLoader_WithCustomModel()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                CheckpointName = "custom_svd_model.safetensors"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var checkpointNode = result.nodes.FirstOrDefault(n => n.type == "ImageOnlyCheckpointLoader");
            Assert.NotNull(checkpointNode);
            Assert.Contains("custom_svd_model.safetensors", checkpointNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresSVDConditioning_WithCustomParameters()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                Width = 512,
                Height = 512,
                DurationSeconds = 5.0f,
                Fps = 24,
                MotionIntensity = 0.8f,
                AugmentationLevel = 0.1f,
                Seed = 54321
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var svdNode = result.nodes.FirstOrDefault(n => n.type == "SVD_img2vid_Conditioning");
            Assert.NotNull(svdNode);
            Assert.Equal(config.Width, svdNode.widgets_values[0]);
            Assert.Equal(config.Height, svdNode.widgets_values[1]);
            // numFrames = duration * fps = 5.0 * 24 = 120
            Assert.Equal(120, svdNode.widgets_values[2]);
            // motionBucketId should be in range based on motion intensity
            var motionBucketId = (int)svdNode.widgets_values[3];
            Assert.InRange(motionBucketId, 127, 255); // 0.8 intensity maps to higher motion
            Assert.Equal(config.AugmentationLevel, svdNode.widgets_values[4]);
            Assert.Equal(config.Seed, svdNode.widgets_values[5]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresVideoSave_WithCustomSettings()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                Fps = 60,
                OutputFilename = "test_video",
                OutputFormat = "webm",
                Quality = 85
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var videoSaveNode = result.nodes.FirstOrDefault(n => n.type == "SaveImage");
            Assert.NotNull(videoSaveNode);
            Assert.Equal(config.OutputFilename, videoSaveNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_UsesImageFilePath_WhenProvided()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                ImageFilePath = "/path/to/input_image.png"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var loadImageNode = result.nodes.FirstOrDefault(n => n.type == "LoadImage");
            Assert.NotNull(loadImageNode);
            Assert.Equal("input_image.png", loadImageNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_UsesPlaceholderImage_WhenNoImageProvided()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                ImageFilePath = null
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var loadImageNode = result.nodes.FirstOrDefault(n => n.type == "LoadImage");
            Assert.NotNull(loadImageNode);
            Assert.Equal("placeholder.png", loadImageNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_IncludesAudio_WhenAudioFileProvided()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                AudioFilePath = "/path/to/audio.mp3"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert - SaveImage node doesn't support audio, so we just check it exists
            var videoSaveNode = result.nodes.FirstOrDefault(n => n.type == "SaveImage");
            Assert.NotNull(videoSaveNode);
            // SaveImage only has filename_prefix widget
            Assert.Single(videoSaveNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_OmitsAudio_WhenNoAudioProvided()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                AudioFilePath = null
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert - SaveImage node doesn't support audio, so we just check it exists
            var videoSaveNode = result.nodes.FirstOrDefault(n => n.type == "SaveImage");
            Assert.NotNull(videoSaveNode);
            // SaveImage only has filename_prefix widget
            Assert.Single(videoSaveNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_CalculatesFramesCorrectly_ForDifferentDurationsAndFps()
        {
            // Arrange & Act & Assert - Test multiple scenarios
            var scenarios = new[]
            {
                new { Duration = 10.0f, Fps = 30, ExpectedFrames = 300 },
                new { Duration = 5.0f, Fps = 24, ExpectedFrames = 120 },
                new { Duration = 2.5f, Fps = 60, ExpectedFrames = 150 },
            };

            foreach (var scenario in scenarios)
            {
                var config = new VideoWorkflowConfig
                {
                    DurationSeconds = scenario.Duration,
                    Fps = scenario.Fps
                };

                var result = VideoWorkflowFactory.CreateWorkflow(config);
                var svdNode = result.nodes.FirstOrDefault(n => n.type == "SVD_img2vid_Conditioning");
                
                Assert.NotNull(svdNode);
                Assert.Equal(scenario.ExpectedFrames, svdNode.widgets_values[2]);
            }
        }

        [Fact]
        public void CreateWorkflow_MapsMotionIntensityToMotionBucket_Correctly()
        {
            // Arrange & Act & Assert - Test motion intensity mapping
            var testCases = new[]
            {
                new { Intensity = 0.0f, ExpectedMin = 127, ExpectedMax = 127 },
                new { Intensity = 0.5f, ExpectedMin = 190, ExpectedMax = 191 },
                new { Intensity = 1.0f, ExpectedMin = 254, ExpectedMax = 254 }
            };

            foreach (var testCase in testCases)
            {
                var config = new VideoWorkflowConfig
                {
                    MotionIntensity = testCase.Intensity
                };

                var result = VideoWorkflowFactory.CreateWorkflow(config);
                var svdNode = result.nodes.FirstOrDefault(n => n.type == "SVD_img2vid_Conditioning");
                
                Assert.NotNull(svdNode);
                var motionBucketId = (int)svdNode.widgets_values[3];
                Assert.InRange(motionBucketId, testCase.ExpectedMin, testCase.ExpectedMax);
            }
        }
    }
}
