using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Factory class to create ComfyUI video generation workflow from configuration
    /// Uses Stable Video Diffusion (SVD) for video generation from images
    /// </summary>
    public static class VideoWorkflowFactory
    {
        public static ComfyUIAudioWorkflow CreateWorkflow(VideoWorkflowConfig config)
        {
            var workflow = new ComfyUIAudioWorkflow
            {
                id = Guid.NewGuid().ToString(),
                revision = 0,
                last_node_id = 12,
                last_link_id = 12,
                nodes = new List<ComfyUINode>
                {
                    CreateSVDModelLoaderNode(config),
                    CreateImageLoaderOrGeneratorNode(config),
                    CreateVAEEncodeNode(),
                    CreateSVDSamplerNode(config),
                    CreateVAEDecodeNode(),
                    CreateVideoSaveNode(config)
                },
                links = new List<object[]>
                {
                    // SVDModelLoader MODEL -> SVDSampler
                    new object[] { 1, 1, 0, 4, 0, "MODEL" },
                    // SVDModelLoader CLIP_VISION -> SVDSampler  
                    new object[] { 2, 1, 1, 4, 1, "CLIP_VISION" },
                    // SVDModelLoader VAE -> VAEEncode
                    new object[] { 3, 1, 2, 3, 1, "VAE" },
                    // SVDModelLoader VAE -> VAEDecode
                    new object[] { 4, 1, 2, 5, 1, "VAE" },
                    // ImageLoader -> VAEEncode
                    new object[] { 5, 2, 0, 3, 0, "IMAGE" },
                    // ImageLoader -> SVDSampler (for conditioning)
                    new object[] { 6, 2, 0, 4, 2, "IMAGE" },
                    // VAEEncode -> SVDSampler
                    new object[] { 7, 3, 0, 4, 3, "LATENT" },
                    // SVDSampler -> VAEDecode
                    new object[] { 8, 4, 0, 5, 0, "LATENT" },
                    // VAEDecode -> VideoSave
                    new object[] { 9, 5, 0, 6, 0, "IMAGE" }
                },
                extra = new Dictionary<string, object>(),
                version = "0.4"
            };

            return workflow;
        }

        private static ComfyUINode CreateSVDModelLoaderNode(VideoWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 1,
                type = "ImageOnlyCheckpointLoader",
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
                    new() { name = "CLIP_VISION", type = "CLIP_VISION", links = new List<int> { 2 } },
                    new() { name = "VAE", type = "VAE", links = new List<int> { 3, 4 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "ImageOnlyCheckpointLoader"
                },
                widgets_values = new object[] { config.CheckpointName }
            };
        }

        private static ComfyUINode CreateImageLoaderOrGeneratorNode(VideoWorkflowConfig config)
        {
            // If an image file path is provided, use LoadImage node
            // Otherwise, we would need to generate an image first (not implemented in this minimal version)
            return new ComfyUINode
            {
                id = 2,
                type = "LoadImage",
                pos = new[] { 100, 250 },
                size = new[] { 315, 314 },
                order = 1,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "image", type = "STRING", widget = new ComfyUIWidget { name = "image" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "IMAGE", type = "IMAGE", links = new List<int> { 5, 6 } },
                    new() { name = "MASK", type = "MASK", links = new List<int>() }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "LoadImage"
                },
                // Use the image file path if provided, otherwise use a placeholder
                widgets_values = new object[] 
                { 
                    !string.IsNullOrEmpty(config.ImageFilePath) 
                        ? Path.GetFileName(config.ImageFilePath) 
                        : "placeholder.png" 
                }
            };
        }

        private static ComfyUINode CreateVAEEncodeNode()
        {
            return new ComfyUINode
            {
                id = 3,
                type = "VAEEncode",
                pos = new[] { 500, 300 },
                size = new[] { 210, 46 },
                order = 2,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "pixels", type = "IMAGE", link = 5 },
                    new() { name = "vae", type = "VAE", link = 3 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 7 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VAEEncode"
                },
                widgets_values = Array.Empty<object>()
            };
        }

        private static ComfyUINode CreateSVDSamplerNode(VideoWorkflowConfig config)
        {
            // Calculate number of frames from duration and FPS
            var numFrames = (int)(config.DurationSeconds * config.Fps);
            
            // Map motion intensity to motion bucket ID (SVD parameter)
            // Motion bucket ID typically ranges from 1-255, where higher = more motion
            var motionBucketId = (int)(config.MotionIntensity * 127) + 127;
            
            return new ComfyUINode
            {
                id = 4,
                type = "SVD_img2vid_Conditioning",
                pos = new[] { 750, 200 },
                size = new[] { 315, 218 },
                order = 3,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip_vision", type = "CLIP_VISION", link = 2 },
                    new() { name = "init_image", type = "IMAGE", link = 6 },
                    new() { name = "vae", type = "VAE", link = 3 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "positive", type = "CONDITIONING", links = new List<int> { 10 } },
                    new() { name = "negative", type = "CONDITIONING", links = new List<int> { 11 } },
                    new() { name = "latent", type = "LATENT", links = new List<int> { 12 } }
                },
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "SVD_img2vid_Conditioning"
                },
                widgets_values = new object[] 
                { 
                    config.Width,
                    config.Height,
                    numFrames,
                    motionBucketId,
                    config.CFGScale,
                    config.Seed == -1 ? Random.Shared.NextInt64(0, long.MaxValue) : config.Seed
                }
            };
        }

        private static ComfyUINode CreateKSamplerNode(VideoWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 7,
                type = "KSampler",
                pos = new[] { 1100, 200 },
                size = new[] { 315, 262 },
                order = 4,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 1 },
                    new() { name = "positive", type = "CONDITIONING", link = 10 },
                    new() { name = "negative", type = "CONDITIONING", link = 11 },
                    new() { name = "latent_image", type = "LATENT", link = 12 }
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
                    config.Seed == -1 ? "randomize" : "fixed",
                    config.Steps,
                    config.CFGScale,
                    "euler",  // sampler
                    "simple", // scheduler
                    1.0       // denoise
                }
            };
        }

        private static ComfyUINode CreateVAEDecodeNode()
        {
            return new ComfyUINode
            {
                id = 5,
                type = "VAEDecode",
                pos = new[] { 1450, 250 },
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

        private static ComfyUINode CreateVideoSaveNode(VideoWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 6,
                type = "VHS_VideoCombine",
                pos = new[] { 1700, 200 },
                size = new[] { 315, 290 },
                order = 6,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "images", type = "IMAGE", link = 9 }
                },
                outputs = new List<ComfyUIOutput>(),
                properties = new Dictionary<string, object>
                {
                    ["Node name for S&R"] = "VHS_VideoCombine"
                },
                widgets_values = new object[] 
                { 
                    config.Fps,
                    0, // loop_count (0 = no loop)
                    config.OutputFilename,
                    config.OutputFormat,
                    false, // pingpong
                    false, // save_image
                    !string.IsNullOrEmpty(config.AudioFilePath) 
                        ? Path.GetFileName(config.AudioFilePath) 
                        : null,
                    config.Quality
                }
            };
        }
    }
}
