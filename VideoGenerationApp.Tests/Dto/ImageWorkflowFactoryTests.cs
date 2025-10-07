using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class ImageWorkflowFactoryTests
    {
        [Fact]
        public void CreateWorkflow_ReturnsValidWorkflow_WithDefaultConfig()
        {
            // Arrange
            var config = new ImageWorkflowConfig();

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

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
            var config = new ImageWorkflowConfig();

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var nodeTypes = result.nodes.Select(n => n.type).ToList();
            
            Assert.Contains("CheckpointLoaderSimple", nodeTypes);
            Assert.Contains("EmptyLatentImage", nodeTypes);
            Assert.Contains("CLIPTextEncode", nodeTypes);
            Assert.Contains("KSampler", nodeTypes);
            Assert.Contains("VAEDecode", nodeTypes);
            Assert.Contains("SaveImage", nodeTypes);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresCheckpointLoader_WithCustomModel()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                CheckpointName = "custom_model.safetensors"
            };

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var checkpointNode = result.nodes.FirstOrDefault(n => n.type == "CheckpointLoaderSimple");
            Assert.NotNull(checkpointNode);
            Assert.Contains("custom_model.safetensors", checkpointNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresKSampler_WithCustomParameters()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Seed = 12345,
                Steps = 30,
                CFGScale = 8.5f,
                SamplerName = "dpmpp_2m",
                Scheduler = "karras",
                Denoise = 0.95f
            };

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var kSamplerNode = result.nodes.FirstOrDefault(n => n.type == "KSampler");
            Assert.NotNull(kSamplerNode);
            Assert.Equal(12345L, kSamplerNode.widgets_values[0]);
            Assert.Equal(30, kSamplerNode.widgets_values[2]);
            Assert.Equal(8.5f, kSamplerNode.widgets_values[3]);
            Assert.Equal("dpmpp_2m", kSamplerNode.widgets_values[4]);
            Assert.Equal("karras", kSamplerNode.widgets_values[5]);
            Assert.Equal(0.95f, kSamplerNode.widgets_values[6]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresEmptyLatentImage_WithCustomDimensions()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Width = 768,
                Height = 512,
                BatchSize = 2
            };

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var latentImageNode = result.nodes.FirstOrDefault(n => n.type == "EmptyLatentImage");
            Assert.NotNull(latentImageNode);
            Assert.Equal(768, latentImageNode.widgets_values[0]);
            Assert.Equal(512, latentImageNode.widgets_values[1]);
            Assert.Equal(2, latentImageNode.widgets_values[2]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresTextEncode_WithCustomPrompts()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                PositivePrompt = "a beautiful sunset over mountains",
                NegativePrompt = "blurry, low quality"
            };

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var positiveNode = result.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 6);
            Assert.NotNull(positiveNode);
            Assert.Equal("a beautiful sunset over mountains", positiveNode.widgets_values[0]);

            var negativeNode = result.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 7);
            Assert.NotNull(negativeNode);
            Assert.Equal("blurry, low quality", negativeNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresSaveImage_WithCustomFilename()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                OutputFilename = "custom_output"
            };

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var saveNode = result.nodes.FirstOrDefault(n => n.type == "SaveImage");
            Assert.NotNull(saveNode);
            Assert.Equal("custom_output", saveNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_HasCorrectNodeCount()
        {
            // Arrange
            var config = new ImageWorkflowConfig();

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.Equal(7, result.nodes.Count);
        }

        [Fact]
        public void CreateWorkflow_HasCorrectLinkCount()
        {
            // Arrange
            var config = new ImageWorkflowConfig();

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.Equal(9, result.links.Count);
        }

        [Fact]
        public void CreateWorkflow_AllNodesHaveValidIds()
        {
            // Arrange
            var config = new ImageWorkflowConfig();

            // Act
            var result = ImageWorkflowFactory.CreateWorkflow(config);

            // Assert
            var nodeIds = result.nodes.Select(n => n.id).ToList();
            Assert.All(nodeIds, id => Assert.True(id > 0));
            Assert.Equal(nodeIds.Count, nodeIds.Distinct().Count()); // All IDs are unique
        }
    }
}
