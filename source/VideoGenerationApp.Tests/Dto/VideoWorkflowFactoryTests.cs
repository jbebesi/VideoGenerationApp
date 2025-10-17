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
            
            Assert.Contains("LoadImage", nodeTypes);
            Assert.Contains("CheckpointLoaderSimple", nodeTypes);
            Assert.Contains("VAEEncode", nodeTypes);
            Assert.Contains("KSampler", nodeTypes);
            Assert.Contains("CLIPTextEncode", nodeTypes);
            Assert.Contains("VAEDecode", nodeTypes);
            Assert.Contains("SaveImage", nodeTypes);
            
            // Should have exactly 8 nodes
            Assert.Equal(8, result.nodes.Count);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresCheckpointLoader_WithCustomModel()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                CheckpointName = "custom_model.safetensors"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var checkpointNode = result.nodes.FirstOrDefault(n => n.type == "CheckpointLoaderSimple");
            Assert.NotNull(checkpointNode);
            Assert.Contains("custom_model.safetensors", checkpointNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresKSampler_WithCustomParameters()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                Seed = 12345,
                Steps = 25,
                CFGScale = 8.5f,
                SamplerName = "euler_ancestral",
                Scheduler = "karras",
                Denoise = 0.8f
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var ksamplerNode = result.nodes.FirstOrDefault(n => n.type == "KSampler");
            Assert.NotNull(ksamplerNode);
            
            // Check that the configuration values are set
            Assert.Equal(config.Seed, ksamplerNode.widgets_values[0]);
            Assert.Equal(config.Steps, ksamplerNode.widgets_values[2]);
            Assert.Equal(config.CFGScale, ksamplerNode.widgets_values[3]);
            Assert.Equal(config.SamplerName, ksamplerNode.widgets_values[4]);
            Assert.Equal(config.Scheduler, ksamplerNode.widgets_values[5]);
            Assert.Equal(config.Denoise, ksamplerNode.widgets_values[6]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresTextEncodeNodes_WithPrompts()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                AnimationStyle = "a beautiful landscape",
                NegativePrompt = "blurry, ugly"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var textEncodeNodes = result.nodes.Where(n => n.type == "CLIPTextEncode").ToList();
            Assert.Equal(2, textEncodeNodes.Count);
            
            // Find positive and negative prompt nodes by their text content
            var positiveNode = textEncodeNodes.FirstOrDefault(n => n.widgets_values.Contains(config.AnimationStyle));
            var negativeNode = textEncodeNodes.FirstOrDefault(n => n.widgets_values.Contains(config.NegativePrompt));
            
            Assert.NotNull(positiveNode);
            Assert.NotNull(negativeNode);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresSaveImage_WithCustomSettings()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                OutputFilename = "test_output"
            };

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var saveImageNode = result.nodes.FirstOrDefault(n => n.type == "SaveImage");
            Assert.NotNull(saveImageNode);
            Assert.Equal(config.OutputFilename, saveImageNode.widgets_values[0]);
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
        public void CreateWorkflow_HasCorrectNodeConnections_WhenCalled()
        {
            // Arrange
            var config = new VideoWorkflowConfig();

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            // Verify the workflow has the expected number of links connecting the nodes
            Assert.NotEmpty(result.links);
            Assert.True(result.links.Count >= 7); // Should have at least 7 connections for the 8-node workflow
        }

        [Fact]
        public void CreateWorkflow_SetsDefaultValues_WhenNotProvided()
        {
            // Arrange
            var config = new VideoWorkflowConfig(); // Use defaults

            // Act
            var result = VideoWorkflowFactory.CreateWorkflow(config);

            // Assert
            var ksamplerNode = result.nodes.FirstOrDefault(n => n.type == "KSampler");
            Assert.NotNull(ksamplerNode);
            
            // Should use default values
            Assert.Equal(20, ksamplerNode.widgets_values[2]); // default steps
            Assert.Equal(7.0f, ksamplerNode.widgets_values[3]); // default CFG scale
            Assert.Equal("euler", ksamplerNode.widgets_values[4]); // default sampler
            Assert.Equal("normal", ksamplerNode.widgets_values[5]); // default scheduler
            Assert.Equal(1.0f, ksamplerNode.widgets_values[6]); // default denoise
        }
    }
}