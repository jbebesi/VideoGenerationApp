using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class ComfyUIWorkflowDtoTests
    {
        [Fact]
        public void CreateBasicVideoWorkflow_ShouldCreateValidWorkflow()
        {
            // Arrange
            var config = new VideoWorkflowConfig
            {
                CheckpointName = "test_checkpoint.safetensors",
                TextPrompt = "a beautiful landscape",
                NegativePrompt = "bad quality, blurry",
                Seed = 12345,
                Steps = 20,
                CFGScale = 7.5f,
                SamplerName = "euler",
                Scheduler = "normal",
                Denoise = 1.0f,
                ImageFilePath = "test_image.png",
                OutputFilename = "test_output"
            };

            // Act
            var workflow = ComfyUIWorkflowFactory.CreateBasicVideoWorkflow(config);

            // Assert
            Assert.NotNull(workflow);
            Assert.NotEmpty(workflow.Id);
            Assert.Equal(0.4, workflow.Version);
            Assert.True(workflow.Nodes.Count > 0);
            Assert.True(workflow.Links.Count > 0);
        }

        [Fact]
        public void ComfyUIWorkflowDto_ShouldSerializeToJson()
        {
            // Arrange
            var workflow = ComfyUIWorkflowFactory.CreateBasicVideoWorkflow();

            // Act
            var json = ComfyUIWorkflowSerializer.ToJson(workflow);
            var deserializedWorkflow = ComfyUIWorkflowSerializer.FromJson(json);

            // Assert
            Assert.NotNull(json);
            Assert.NotNull(deserializedWorkflow);
            Assert.Equal(workflow.Id, deserializedWorkflow.Id);
            Assert.Equal(workflow.Version, deserializedWorkflow.Version);
            Assert.Equal(workflow.Nodes.Count, deserializedWorkflow.Nodes.Count);
            Assert.Equal(workflow.Links.Count, deserializedWorkflow.Links.Count);
        }

        [Fact]
        public void ComfyUIWorkflowNode_ShouldHaveCorrectStructure()
        {
            // Arrange
            var workflow = ComfyUIWorkflowFactory.CreateBasicVideoWorkflow();

            // Act
            var loadImageNode = workflow.Nodes.First(n => n.Type == "LoadImage");
            var kSamplerNode = workflow.Nodes.First(n => n.Type == "KSampler");

            // Assert
            Assert.NotNull(loadImageNode);
            Assert.Equal("LoadImage", loadImageNode.Type);
            Assert.NotEmpty(loadImageNode.Inputs);
            Assert.NotEmpty(loadImageNode.Outputs);
            Assert.NotEmpty(loadImageNode.WidgetValues);

            Assert.NotNull(kSamplerNode);
            Assert.Equal("KSampler", kSamplerNode.Type);
            Assert.Equal(10, kSamplerNode.Inputs.Count); // model, positive, negative, latent_image, seed, steps, cfg, sampler_name, scheduler, denoise
            Assert.Single(kSamplerNode.Outputs); // LATENT output
            Assert.Equal(7, kSamplerNode.WidgetValues.Count); // All the widget values
        }

        [Fact]
        public void ComfyUIWorkflowLinks_ShouldConnectNodesCorrectly()
        {
            // Arrange
            var workflow = ComfyUIWorkflowFactory.CreateBasicVideoWorkflow();

            // Act
            var imageToVaeLink = workflow.Links.FirstOrDefault(l => l.DataType == "IMAGE");
            var modelToKSamplerLink = workflow.Links.FirstOrDefault(l => l.DataType == "MODEL");

            // Assert
            Assert.NotNull(imageToVaeLink);
            Assert.Equal("IMAGE", imageToVaeLink.DataType);

            Assert.NotNull(modelToKSamplerLink);
            Assert.Equal("MODEL", modelToKSamplerLink.DataType);
        }

        [Fact]
        public void ComfyUIWorkflowExtensions_AddNode_ShouldWorkCorrectly()
        {
            // Arrange
            var workflow = new ComfyUIWorkflowDto();

            // Act
            var node = workflow.AddNode("TestNode", 100, 200);

            // Assert
            Assert.Single(workflow.Nodes);
            Assert.Equal("TestNode", node.Type);
            Assert.Equal(100, node.Position[0]);
            Assert.Equal(200, node.Position[1]);
            Assert.True(node.Id >= 0); // Node should have a valid ID
        }

        [Fact]
        public void ComfyUIWorkflowExtensions_AddLink_ShouldWorkCorrectly()
        {
            // Arrange
            var workflow = new ComfyUIWorkflowDto();

            // Act
            workflow.AddLink(1, 0, 2, 1, "TEST_TYPE");

            // Assert
            Assert.Single(workflow.Links);
            var link = workflow.Links.First();
            Assert.Equal(1, link.SourceNodeId);
            Assert.Equal(0, link.SourceOutputIndex);
            Assert.Equal(2, link.TargetNodeId);
            Assert.Equal(1, link.TargetInputIndex);
            Assert.Equal("TEST_TYPE", link.DataType);
        }

        [Fact]
        public void ComfyUIWorkflowDto_ShouldHaveCorrectBasicStructure()
        {
            // Arrange
            var workflow = ComfyUIWorkflowFactory.CreateBasicVideoWorkflow();

            // Act & Assert
            Assert.Equal(8, workflow.Nodes.Count);
            Assert.Equal(11, workflow.Links.Count);
            
            // Check that we have the expected node types
            var nodeTypes = workflow.Nodes.Select(n => n.Type).ToList();
            Assert.Contains("LoadImage", nodeTypes);
            Assert.Contains("CheckpointLoaderSimple", nodeTypes);
            Assert.Contains("VAEEncode", nodeTypes);
            Assert.Contains("KSampler", nodeTypes);
            Assert.Contains("VAEDecode", nodeTypes);
            Assert.Contains("SaveImage", nodeTypes);
            Assert.Equal(2, nodeTypes.Count(t => t == "CLIPTextEncode")); // Positive and negative prompts
        }
    }
}