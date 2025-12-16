using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Factory class to create ComfyUI image generation workflow from configuration
    /// </summary>
    public static class ImageWorkflowFactory
    {
        public static ComfyUIAudioWorkflow CreateWorkflow(ImageWorkflowConfig config)
        {
            // Check if this is a Qwen-Image model set
            var modelSet = config.GetCurrentModelSet();
            
            if (modelSet.CheckpointName.Contains("qwen_image"))
            {
                // Use Qwen-Image workflow structure
                return CreateQwenImageWorkflow(config, modelSet);
            }
            else
            {
                // Use traditional checkpoint-based workflow
                return CreateTraditionalWorkflow(config);
            }
        }

        private static ComfyUIAudioWorkflow CreateQwenImageWorkflow(ImageWorkflowConfig config, ImageModelSetConfig modelSet)
        {
            var workflow = new ComfyUIAudioWorkflow
            {
                id = Guid.NewGuid().ToString(),
                revision = 0,
                last_node_id = 74,
                last_link_id = 130,
                nodes = new List<ComfyUINode>(),
                links = new List<object[]>(),
                extra = new Dictionary<string, object>(),
                version = "0.4"
            };

            var nodes = workflow.nodes;
            var links = workflow.links;

            // UNETLoader (id: 37)
            nodes.Add(new ComfyUINode
            {
                id = 37,
                type = "UNETLoader",
                pos = new[] { 20, 50 },
                size = new[] { 330, 90 },
                order = 5,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 129 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "UNETLoader",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { modelSet.UNetModel, "default" }
            });

            // CLIPLoader (id: 38)
            nodes.Add(new ComfyUINode
            {
                id = 38,
                type = "CLIPLoader",
                pos = new[] { 20, 190 },
                size = new[] { 330, 110 },
                order = 1,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CLIP", type = "CLIP", links = new List<int> { 74, 75 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CLIPLoader",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { modelSet.CLIPModel, modelSet.CLIPType, "default" }
            });

            // VAELoader (id: 39)
            nodes.Add(new ComfyUINode
            {
                id = 39,
                type = "VAELoader",
                pos = new[] { 20, 340 },
                size = new[] { 330, 60 },
                order = 0,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "VAE", type = "VAE", links = new List<int> { 76 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VAELoader",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { modelSet.VAEModel }
            });

            // EmptySD3LatentImage (id: 58)
            nodes.Add(new ComfyUINode
            {
                id = 58,
                type = "EmptySD3LatentImage",
                pos = new[] { 50, 510 },
                size = new[] { 270, 106 },
                order = 2,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 107 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "EmptySD3LatentImage",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { config.Width, config.Height, config.BatchSize }
            });

            // CLIPTextEncode Positive (id: 6)
            nodes.Add(new ComfyUINode
            {
                id = 6,
                type = "CLIPTextEncode",
                pos = new[] { 390, 240 },
                size = new[] { 422, 164 },
                order = 9,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 74 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 46 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CLIPTextEncode",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { config.PositivePrompt },
                color = "#232",
                bgcolor = "#353"
            });

            // CLIPTextEncode Negative (id: 7)
            nodes.Add(new ComfyUINode
            {
                id = 7,
                type = "CLIPTextEncode",
                pos = new[] { 390, 440 },
                size = new[] { 425, 180 },
                order = 10,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 75 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 52 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CLIPTextEncode",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { config.NegativePrompt },
                color = "#322",
                bgcolor = "#533"
            });

            // Optional LoRA loader (id: 73)
            int modelNodeId = 37; // Default to UNETLoader
            if (!string.IsNullOrEmpty(modelSet.LoRAModel))
            {
                nodes.Add(new ComfyUINode
                {
                    id = 73,
                    type = "LoraLoaderModelOnly",
                    pos = new[] { 460, 60 },
                    size = new[] { 270, 82 },
                    order = 11,
                    mode = 0,
                    inputs = new List<ComfyUIInput>
                    {
                        new() { name = "model", type = "MODEL", link = 129 }
                    },
                    outputs = new List<ComfyUIOutput>
                    {
                        new() { name = "MODEL", type = "MODEL", links = new List<int> { 130 } }
                    },
                    properties = new Dictionary<string, object>
                    {
                        ["Node name for S&R"] = "LoraLoaderModelOnly",
                        ["cnr_id"] = "comfy-core",
                        ["ver"] = "0.3.49"
                    },
                    widgets_values = new object[] { modelSet.LoRAModel, modelSet.LoRAStrength }
                });
                modelNodeId = 73; // Use LoRA output
            }

            // ModelSamplingAuraFlow (id: 66)
            nodes.Add(new ComfyUINode
            {
                id = 66,
                type = "ModelSamplingAuraFlow",
                pos = new[] { 850, 10 },
                size = new[] { 300, 58 },
                order = 12,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = modelNodeId == 73 ? 130 : 129 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 125 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "ModelSamplingAuraFlow",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { modelSet.ModelSamplingShift }
            });

            // KSampler (id: 3)
            nodes.Add(new ComfyUINode
            {
                id = 3,
                type = "KSampler",
                pos = new[] { 850, 120 },
                size = new[] { 300, 474 },
                order = 13,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 125 },
                    new() { name = "positive", type = "CONDITIONING", link = 46 },
                    new() { name = "negative", type = "CONDITIONING", link = 52 },
                    new() { name = "latent_image", type = "LATENT", link = 107 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 128 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "KSampler",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[]
                {
                    config.Seed == -1 ? Random.Shared.NextInt64(0, long.MaxValue) : config.Seed,
                    config.Seed == -1 ? "randomize" : "fixed",
                    config.Steps,
                    config.CFGScale,
                    config.SamplerName,
                    config.Scheduler,
                    config.Denoise
                }
            });

            // VAEDecode (id: 8)
            nodes.Add(new ComfyUINode
            {
                id = 8,
                type = "VAEDecode",
                pos = new[] { 1170, -90 },
                size = new[] { 210, 46 },
                flags = new Dictionary<string, object> { { "collapsed", false } },
                order = 14,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "samples", type = "LATENT", link = 128 },
                    new() { name = "vae", type = "VAE", link = 76 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "IMAGE", type = "IMAGE", links = new List<int> { 110 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VAEDecode",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = Array.Empty<object>()
            });

            // SaveImage (id: 60)
            nodes.Add(new ComfyUINode
            {
                id = 60,
                type = "SaveImage",
                pos = new[] { 1170, 10 },
                size = new[] { 490, 600 },
                order = 15,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "images", type = "IMAGE", link = 110 }
                },
                outputs = new List<ComfyUIOutput>(),
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "SaveImage",
                    ["cnr_id"] = "comfy-core",
                    ["ver"] = "0.3.48"
                },
                widgets_values = new object[] { config.OutputFilename }
            });

            // Add links
            links.AddRange(new[]
            {
                new object[] { 46, 6, 0, 3, 1, "CONDITIONING" },
                new object[] { 52, 7, 0, 3, 2, "CONDITIONING" },
                new object[] { 74, 38, 0, 6, 0, "CLIP" },
                new object[] { 75, 38, 0, 7, 0, "CLIP" },
                new object[] { 76, 39, 0, 8, 1, "VAE" },
                new object[] { 107, 58, 0, 3, 3, "LATENT" },
                new object[] { 110, 8, 0, 60, 0, "IMAGE" },
                new object[] { 125, 66, 0, 3, 0, "MODEL" },
                new object[] { 128, 3, 0, 8, 0, "LATENT" },
                new object[] { 129, 37, 0, modelNodeId == 73 ? 73 : 66, 0, "MODEL" }
            });

            if (modelNodeId == 73)
            {
                links.Add(new object[] { 130, 73, 0, 66, 0, "MODEL" });
            }

            return workflow;
        }

        private static ComfyUIAudioWorkflow CreateTraditionalWorkflow(ImageWorkflowConfig config)
        {
            var modelSet = config.GetCurrentModelSet();
            
            // Determine if we need separate VAE loader
            bool useSeparateVAE = !string.IsNullOrEmpty(modelSet.VAEModel);
            int vaeLink = useSeparateVAE ? 10 : 4;
            
            var workflow = new ComfyUIAudioWorkflow
            {
                id = Guid.NewGuid().ToString(),
                revision = 0,
                last_node_id = useSeparateVAE ? 10 : 9,
                last_link_id = useSeparateVAE ? 10 : 9,
                nodes = new List<ComfyUINode>
                {
                    CreateCheckpointLoaderNode(config),
                    CreateEmptyLatentImageNode(config),
                    CreateCLIPTextEncodePositiveNode(config),
                    CreateCLIPTextEncodeNegativeNode(config),
                    CreateKSamplerNode(config),
                    CreateVAEDecodeNode(vaeLink),
                    CreateSaveImageNode(config)
                },
                links = new List<object[]>
                {
                    // CheckpointLoader MODEL -> KSampler
                    new object[] { 1, 4, 0, 3, 0, "MODEL" },
                    // CheckpointLoader CLIP -> CLIPTextEncode (positive)
                    new object[] { 2, 4, 1, 6, 0, "CLIP" },
                    // CheckpointLoader CLIP -> CLIPTextEncode (negative)
                    new object[] { 3, 4, 1, 7, 0, "CLIP" },
                    // EmptyLatentImage -> KSampler
                    new object[] { 5, 5, 0, 3, 3, "LATENT" },
                    // CLIPTextEncode (positive) -> KSampler
                    new object[] { 6, 6, 0, 3, 1, "CONDITIONING" },
                    // CLIPTextEncode (negative) -> KSampler
                    new object[] { 7, 7, 0, 3, 2, "CONDITIONING" },
                    // KSampler -> VAEDecode
                    new object[] { 8, 3, 0, 8, 0, "LATENT" },
                    // VAEDecode -> SaveImage
                    new object[] { 9, 8, 0, 9, 0, "IMAGE" }
                },
                extra = new Dictionary<string, object>(),
                version = "0.4"
            };

            if (useSeparateVAE)
            {
                // Add separate VAE loader
                workflow.nodes.Insert(1, CreateVAELoaderNode(modelSet));
                // Link: VAELoader -> VAEDecode
                workflow.links.Add(new object[] { 10, 10, 0, 8, 1, "VAE" });
            }
            else
            {
                // Link: CheckpointLoader VAE -> VAEDecode
                workflow.links.Add(new object[] { 4, 4, 2, 8, 1, "VAE" });
            }

            return workflow;
        }

        private static ComfyUINode CreateVAELoaderNode(ImageModelSetConfig modelSet)
        {
            return new ComfyUINode
            {
                id = 10,
                type = "VAELoader",
                pos = new[] { 100, 240 },
                size = new[] { 350, 60 },
                order = 0,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "VAE", type = "VAE", links = new List<int> { 10 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VAELoader"
                },
                widgets_values = new object[] { modelSet.VAEModel }
            };
        }

        private static ComfyUINode CreateCheckpointLoaderNode(ImageWorkflowConfig config)
        {
            var modelSet = config.GetCurrentModelSet();
            
            return new ComfyUINode
            {
                id = 4,
                type = "CheckpointLoaderSimple",
                pos = new[] { 100, 100 },
                size = new[] { 350, 100 },
                order = 0,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 1 } },
                    new() { name = "CLIP", type = "CLIP", links = new List<int> { 2, 3 } },
                    new() { name = "VAE", type = "VAE", links = new List<int> { 4 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CheckpointLoaderSimple"
                },
                widgets_values = new object[] { modelSet.CheckpointName }
            };
        }

        private static ComfyUINode CreateEmptyLatentImageNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 5,
                type = "EmptyLatentImage",
                pos = new[] { 500, 400 },
                size = new[] { 315, 106 },
                order = 1,
                mode = 0,
                inputs = new List<ComfyUIInput>(),
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 5 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "EmptyLatentImage"
                },
                widgets_values = new object[] 
                { 
                    config.Width, 
                    config.Height, 
                    config.BatchSize 
                }
            };
        }

        private static ComfyUINode CreateCLIPTextEncodePositiveNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 6,
                type = "CLIPTextEncode",
                pos = new[] { 500, 100 },
                size = new[] { 400, 200 },
                order = 2,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 2 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 6 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CLIPTextEncode"
                },
                widgets_values = new object[] { config.PositivePrompt }
            };
        }

        private static ComfyUINode CreateCLIPTextEncodeNegativeNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 7,
                type = "CLIPTextEncode",
                pos = new[] { 500, 320 },
                size = new[] { 400, 200 },
                order = 3,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 3 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 7 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "CLIPTextEncode"
                },
                widgets_values = new object[] { config.NegativePrompt }
            };
        }

        private static ComfyUINode CreateKSamplerNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 3,
                type = "KSampler",
                pos = new[] { 950, 200 },
                size = new[] { 315, 262 },
                order = 4,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 1 },
                    new() { name = "positive", type = "CONDITIONING", link = 6 },
                    new() { name = "negative", type = "CONDITIONING", link = 7 },
                    new() { name = "latent_image", type = "LATENT", link = 5 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 8 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "KSampler"
                },
                widgets_values = new object[] 
                { 
                    config.Seed == -1 ? Random.Shared.NextInt64(0, long.MaxValue) : config.Seed,
                    config.Seed == -1 ? "randomize" : "fixed",  // control_after_generate
                    config.Steps,
                    config.CFGScale,
                    config.SamplerName,
                    config.Scheduler,
                    config.Denoise
                }
            };
        }

        private static ComfyUINode CreateVAEDecodeNode(int vaeLink)
        {
            return new ComfyUINode
            {
                id = 8,
                type = "VAEDecode",
                pos = new[] { 1300, 250 },
                size = new[] { 210, 46 },
                order = 5,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "samples", type = "LATENT", link = 8 },
                    new() { name = "vae", type = "VAE", link = vaeLink }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "IMAGE", type = "IMAGE", links = new List<int> { 9 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VAEDecode"
                },
                widgets_values = Array.Empty<object>()
            };
        }

        private static ComfyUINode CreateSaveImageNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 9,
                type = "SaveImage",
                pos = new[] { 1550, 200 },
                size = new[] { 315, 270 },
                order = 6,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "images", type = "IMAGE", link = 9 }
                },
                outputs = new List<ComfyUIOutput>(),
                properties = new Dictionary<string, object>(),
                widgets_values = new object[] { config.OutputFilename }
            };
        }
    }
}
