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
            var workflow = new ComfyUIAudioWorkflow
            {
                id = Guid.NewGuid().ToString(),
                revision = 0,
                last_node_id = 9,
                last_link_id = 9,
                nodes = new List<ComfyUINode>
                {
                    CreateCheckpointLoaderNode(config),
                    CreateEmptyLatentImageNode(config),
                    CreateCLIPTextEncodePositiveNode(config),
                    CreateCLIPTextEncodeNegativeNode(config),
                    CreateKSamplerNode(config),
                    CreateVAEDecodeNode(),
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
                    // CheckpointLoader VAE -> VAEDecode
                    new object[] { 4, 4, 2, 8, 1, "VAE" },
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

            return workflow;
        }

        private static ComfyUINode CreateCheckpointLoaderNode(ImageWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 4,
                type = "CheckpointLoaderSimple",
                pos = new[] { 100, 100 },
                size = new[] { 350, 100 },
                order = 0,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "ckpt_name", type = "COMBO", widget = new ComfyUIWidget { name = "ckpt_name" } }
                },
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
                widgets_values = new object[] { config.CheckpointName }
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

        private static ComfyUINode CreateVAEDecodeNode()
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
                    new() { name = "vae", type = "VAE", link = 4 }
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
