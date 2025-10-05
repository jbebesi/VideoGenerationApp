using VideoGenerationApp.Dto;
using Xunit;

namespace VideoGenerationApp.Tests.Dto
{
    public class AudioWorkflowFactoryTests
    {
        [Fact]
        public void CreateWorkflow_ReturnsValidWorkflow_WithDefaultConfig()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

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
            var config = new AudioWorkflowConfig();

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var nodeTypes = result.nodes.Select(n => n.type).ToList();
            
            Assert.Contains("CheckpointLoaderSimple", nodeTypes);
            Assert.Contains("EmptyAceStepLatentAudio", nodeTypes);
            Assert.Contains("TextEncodeAceStepAudio", nodeTypes);
            Assert.Contains("ConditioningZeroOut", nodeTypes);
            Assert.Contains("LatentOperationTonemapReinhard", nodeTypes);
            Assert.Contains("ModelSamplingSD3", nodeTypes);
            Assert.Contains("LatentApplyOperationCFG", nodeTypes);
            Assert.Contains("KSampler", nodeTypes);
            Assert.Contains("VAEDecodeAudio", nodeTypes);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresCheckpointLoader_WithCustomModel()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                CheckpointName = "custom_model.safetensors"
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var checkpointNode = result.nodes.FirstOrDefault(n => n.type == "CheckpointLoaderSimple");
            Assert.NotNull(checkpointNode);
            Assert.Contains("custom_model.safetensors", checkpointNode.widgets_values);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresTextEncodeAceStep_WithCustomTagsAndLyrics()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Tags = "rock, heavy metal, guitar solo",
                Lyrics = "[verse]\nRocking all night long\n[chorus]\nWe are the champions",
                LyricsStrength = 0.85f
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var textEncodeNode = result.nodes.FirstOrDefault(n => n.type == "TextEncodeAceStepAudio");
            Assert.NotNull(textEncodeNode);
            Assert.Equal("rock, heavy metal, guitar solo", textEncodeNode.widgets_values[0]);
            Assert.Equal("[verse]\nRocking all night long\n[chorus]\nWe are the champions", textEncodeNode.widgets_values[1]);
            Assert.Equal(0.85f, textEncodeNode.widgets_values[2]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresEmptyAceStepLatentAudio_WithCustomDurationAndBatch()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                AudioDurationSeconds = 90f,
                BatchSize = 2
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var latentAudioNode = result.nodes.FirstOrDefault(n => n.type == "EmptyAceStepLatentAudio");
            Assert.NotNull(latentAudioNode);
            Assert.Equal(90f, latentAudioNode.widgets_values[0]);
            Assert.Equal(2, latentAudioNode.widgets_values[1]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresKSampler_WithCustomSamplingSettings()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Seed = 54321,
                Steps = 35,
                CFGScale = 8.5f,
                SamplerName = "dpmpp_2m",
                Scheduler = "karras",
                Denoise = 0.9f
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var kSamplerNode = result.nodes.FirstOrDefault(n => n.type == "KSampler");
            Assert.NotNull(kSamplerNode);
            Assert.Equal(54321L, kSamplerNode.widgets_values[0]);
            Assert.Equal("fixed", kSamplerNode.widgets_values[1]); // control_after_generate
            Assert.Equal(35, kSamplerNode.widgets_values[2]);
            Assert.Equal(8.5f, kSamplerNode.widgets_values[3]);
            Assert.Equal("dpmpp_2m", kSamplerNode.widgets_values[4]);
            Assert.Equal("karras", kSamplerNode.widgets_values[5]);
            Assert.Equal(0.9f, kSamplerNode.widgets_values[6]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresModelSamplingSD3_WithCustomShift()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                ModelShift = 3.5f
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var modelSamplingNode = result.nodes.FirstOrDefault(n => n.type == "ModelSamplingSD3");
            Assert.NotNull(modelSamplingNode);
            Assert.Equal(3.5f, modelSamplingNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresTonemapReinhard_WithCustomMultiplier()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                TonemapMultiplier = 1.5f
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var tonemapNode = result.nodes.FirstOrDefault(n => n.type == "LatentOperationTonemapReinhard");
            Assert.NotNull(tonemapNode);
            Assert.Equal(1.5f, tonemapNode.widgets_values[0]);
        }

        [Fact]
        public void CreateWorkflow_ConfiguresSaveAudio_WithCustomOutputSettings()
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                OutputFilename = "custom_output/my_song",
                OutputFormat = "mp3",
                AudioQuality = "V2"
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            // Check for SaveAudio or SaveAudioMP3 node
            var saveAudioNode = result.nodes.FirstOrDefault(n => n.type == "SaveAudio" || n.type == "SaveAudioMP3");
            Assert.NotNull(saveAudioNode);
            
            if (saveAudioNode.type == "SaveAudioMP3")
            {
                Assert.Equal("custom_output/my_song", saveAudioNode.widgets_values[0]);
                Assert.Equal("V2", saveAudioNode.widgets_values[1]);
            }
            else
            {
                Assert.Equal("custom_output/my_song", saveAudioNode.widgets_values[0]);
            }
        }

        [Fact]
        public void CreateWorkflow_GeneratesUniqueIds_OnMultipleCalls()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            var result1 = AudioWorkflowFactory.CreateWorkflow(config);
            var result2 = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.NotEqual(result1.id, result2.id);
        }

        [Fact]
        public void CreateWorkflow_HasCorrectLinkStructure_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            Assert.NotEmpty(result.links);
            
            // Each link should be an array with 6 elements: [link_id, source_node_id, source_slot, target_node_id, target_slot, type]
            foreach (var link in result.links)
            {
                var linkArray = link as object[];
                Assert.NotNull(linkArray);
                Assert.Equal(6, linkArray.Length);
            }
        }

        [Fact]
        public void CreateWorkflow_SetsCorrectNodePositions_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            foreach (var node in result.nodes)
            {
                Assert.NotNull(node.pos);
                Assert.Equal(2, node.pos.Length); // x, y coordinates
                Assert.NotNull(node.size);
                Assert.Equal(2, node.size.Length); // width, height
            }
        }

        [Fact]
        public void CreateWorkflow_SetsCorrectNodeIds_WhenCalled()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var nodeIds = result.nodes.Select(n => n.id).ToList();
            
            // All node IDs should be unique
            Assert.Equal(nodeIds.Count, nodeIds.Distinct().Count());
            
            // All node IDs should be positive
            Assert.All(nodeIds, id => Assert.True(id > 0));
        }

        [Theory]
        [InlineData(-1, "randomize")] // Random seed
        [InlineData(0, "fixed")]
        [InlineData(12345, "fixed")]
        public void CreateWorkflow_HandlesSeedCorrectly_ForDifferentValues(long seed, string expectedControl)
        {
            // Arrange
            var config = new AudioWorkflowConfig
            {
                Seed = seed
            };

            // Act
            var result = AudioWorkflowFactory.CreateWorkflow(config);

            // Assert
            var kSamplerNode = result.nodes.FirstOrDefault(n => n.type == "KSampler");
            Assert.NotNull(kSamplerNode);
            
            if (seed == -1)
            {
                // For random seed, should use a generated positive number
                Assert.True((long)kSamplerNode.widgets_values[0] > 0);
            }
            else
            {
                Assert.Equal(seed, kSamplerNode.widgets_values[0]);
            }
            
            Assert.Equal(expectedControl, kSamplerNode.widgets_values[1]);
        }

        [Fact]
        public void AudioWorkflowConfig_HasCorrectDefaults_WhenInstantiated()
        {
            // Act
            var config = new AudioWorkflowConfig();

            // Assert
            Assert.Equal("ace_step_v1_3.5b.safetensors", config.CheckpointName);
            Assert.Equal("pop, female voice, catchy melody", config.Tags);
            Assert.Contains("[verse]", config.Lyrics);
            Assert.Contains("[chorus]", config.Lyrics);
            Assert.Equal(0.99f, config.LyricsStrength);
            Assert.Equal(120f, config.AudioDurationSeconds);
            Assert.Equal(1, config.BatchSize);
            Assert.Equal(5.0f, config.ModelShift);
            Assert.Equal(1.0f, config.TonemapMultiplier);
            Assert.Equal(-1, config.Seed);
            Assert.Equal(50, config.Steps);
            Assert.Equal(5.0f, config.CFGScale);
            Assert.Equal("euler", config.SamplerName);
            Assert.Equal("simple", config.Scheduler);
            Assert.Equal(1.0f, config.Denoise);
            Assert.Equal("audio/ComfyUI", config.OutputFilename);
            Assert.Equal("mp3", config.OutputFormat);
            Assert.Equal("V0", config.AudioQuality);
        }
    }
}