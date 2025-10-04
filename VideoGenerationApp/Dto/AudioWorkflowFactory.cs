namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Configuration parameters for audio generation workflow
    /// </summary>
    public class AudioWorkflowConfig
    {
        // Checkpoint/Model settings
        public string CheckpointName { get; set; } = "stable-audio-open-1.0.safetensors";
        public string ClipName { get; set; } = "t5-base.safetensors"; // Updated to valid model

        // Text prompts
        public string PositivePrompt { get; set; } = "heaven church electronic dance music";
        public string NegativePrompt { get; set; } = "";

        // Audio settings
        public float AudioDurationSeconds { get; set; } = 47.6f;
        public int BatchSize { get; set; } = 1;

        // Sampling settings
        public long Seed { get; set; } = -1; // -1 for random
        public int Steps { get; set; } = 50;
        public float CFGScale { get; set; } = 4.98f;
        public string SamplerName { get; set; } = "dpmpp_3m_sde_gpu";
        public string Scheduler { get; set; } = "exponential";
        public float Denoise { get; set; } = 1.0f;

        // Output settings
        public string FilenamePrefix { get; set; } = "ComfyUI";
    }

    /// <summary>
    /// Factory class to create ComfyUI audio workflow from configuration
    /// </summary>
    public static class AudioWorkflowFactory
    {
        public static ComfyUIAudioWorkflow CreateWorkflow(AudioWorkflowConfig config)
        {
            var workflow = new ComfyUIAudioWorkflow();

            // Add KSampler node (id: 3)
            workflow.nodes.Add(CreateKSamplerNode(config));

            // Add CheckpointLoaderSimple node (id: 4)
            workflow.nodes.Add(CreateCheckpointLoaderNode(config));

            // Add CLIPLoader node (id: 5)
            workflow.nodes.Add(CreateClipLoaderNode(config));

            // Add Positive CLIPTextEncode node (id: 6)
            workflow.nodes.Add(CreatePositiveTextEncodeNode(config));

            // Add Negative CLIPTextEncode node (id: 7)
            workflow.nodes.Add(CreateNegativeTextEncodeNode(config));

            // Add EmptyLatentAudio node (id: 11)
            workflow.nodes.Add(CreateEmptyLatentAudioNode(config));

            // Add VAEDecodeAudio node (id: 12)
            workflow.nodes.Add(CreateVAEDecodeAudioNode());

            // Add SaveAudio node (id: 13)
            workflow.nodes.Add(CreateSaveAudioNode(config));

            return workflow;
        }

        private static ComfyUINode CreateKSamplerNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 3,
                type = "KSampler",
                pos = new[] { 864, 96 },
                size = new[] { 315, 262 },
                order = 6,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 18 },
                    new() { name = "positive", type = "CONDITIONING", link = 4 },
                    new() { name = "negative", type = "CONDITIONING", link = 6 },
                    new() { name = "latent_image", type = "LATENT", link = 12 },
                    new() { name = "seed", type = "INT", widget = new ComfyUIWidget { name = "seed" } },
                    new() { name = "steps", type = "INT", widget = new ComfyUIWidget { name = "steps" } },
                    new() { name = "cfg", type = "FLOAT", widget = new ComfyUIWidget { name = "cfg" } },
                    new() { name = "sampler_name", type = "COMBO", widget = new ComfyUIWidget { name = "sampler_name" } },
                    new() { name = "scheduler", type = "COMBO", widget = new ComfyUIWidget { name = "scheduler" } },
                    new() { name = "denoise", type = "FLOAT", widget = new ComfyUIWidget { name = "denoise" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", slot_index = 0, links = new List<int> { 13 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "KSampler" } },
                widgets_values = new object[]
                {
                    config.Seed == -1 ? Random.Shared.NextInt64(0, long.MaxValue) : config.Seed, // seed
                    "randomize", // control_after_generate
                    config.Steps, // steps
                    config.CFGScale, // cfg
                    config.SamplerName, // sampler_name
                    config.Scheduler, // scheduler
                    config.Denoise // denoise
                }
            };
        }

        private static ComfyUINode CreateCheckpointLoaderNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 4,
                type = "CheckpointLoaderSimple",
                pos = new[] { 0, 240 },
                size = new[] { 336, 98 },
                order = 0,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "ckpt_name", type = "COMBO", widget = new ComfyUIWidget { name = "ckpt_name" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", slot_index = 0, links = new List<int> { 18 } },
                    new() { name = "CLIP", type = "CLIP", slot_index = 1, links = new List<int>() },
                    new() { name = "VAE", type = "VAE", slot_index = 2, links = new List<int> { 14 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "CheckpointLoaderSimple" } },
                widgets_values = new object[] { config.CheckpointName }
            };
        }

        private static ComfyUINode CreateClipLoaderNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 5,
                type = "CLIPLoader",
                pos = new[] { 0, 384 },
                size = new[] { 336, 98 },
                order = 1,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip_name", type = "COMBO", widget = new ComfyUIWidget { name = "clip_name" } },
                    new() { name = "type", type = "COMBO", widget = new ComfyUIWidget { name = "type" } },
                    new() { name = "device", type = "COMBO", widget = new ComfyUIWidget { name = "device" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CLIP", type = "CLIP", slot_index = 0, links = new List<int> { 25, 26 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "CLIPLoader" } },
                widgets_values = new object[] { config.ClipName, "stable_audio", "default" }
            };
        }

        private static ComfyUINode CreatePositiveTextEncodeNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 6,
                type = "CLIPTextEncode",
                pos = new[] { 384, 96 },
                size = new[] { 432, 144 },
                order = 4,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 25 },
                    new() { name = "text", type = "STRING", widget = new ComfyUIWidget { name = "text" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", slot_index = 0, links = new List<int> { 4 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "CLIPTextEncode" } },
                widgets_values = new object[] { config.PositivePrompt },
                color = "#232",
                bgcolor = "#353"
            };
        }

        private static ComfyUINode CreateNegativeTextEncodeNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 7,
                type = "CLIPTextEncode",
                pos = new[] { 384, 288 },
                size = new[] { 432, 144 },
                order = 5,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 26 },
                    new() { name = "text", type = "STRING", widget = new ComfyUIWidget { name = "text" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", slot_index = 0, links = new List<int> { 6 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "CLIPTextEncode" } },
                widgets_values = new object[] { config.NegativePrompt },
                color = "#322",
                bgcolor = "#533"
            };
        }

        private static ComfyUINode CreateEmptyLatentAudioNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 11,
                type = "EmptyLatentAudio",
                pos = new[] { 576, 480 },
                size = new[] { 240, 82 },
                order = 2,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "seconds", type = "FLOAT", widget = new ComfyUIWidget { name = "seconds" } },
                    new() { name = "batch_size", type = "INT", widget = new ComfyUIWidget { name = "batch_size" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 12 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "EmptyLatentAudio" } },
                widgets_values = new object[] { config.AudioDurationSeconds, config.BatchSize }
            };
        }

        private static ComfyUINode CreateVAEDecodeAudioNode()
        {
            return new ComfyUINode
            {
                id = 12,
                type = "VAEDecodeAudio",
                pos = new[] { 1200, 96 },
                size = new[] { 210, 46 },
                order = 7,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "samples", type = "LATENT", link = 13 },
                    new() { name = "vae", type = "VAE", link = 14 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "AUDIO", type = "AUDIO", slot_index = 0, links = new List<int> { 15 } }
                },
                properties = new Dictionary<string, object> { { "Node name for S&R", "VAEDecodeAudio" } },
                widgets_values = Array.Empty<object>()
            };
        }

        private static ComfyUINode CreateSaveAudioNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 13,
                type = "SaveAudio",
                pos = new[] { 1440, 96 },
                size = new[] { 355, 112 },
                order = 8,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "audio", type = "AUDIO", link = 15 },
                    new() { name = "filename_prefix", type = "STRING", widget = new ComfyUIWidget { name = "filename_prefix" } },
                    new() { name = "audioUI", type = "AUDIO_UI", widget = new ComfyUIWidget { name = "audioUI" } }
                },
                outputs = new List<ComfyUIOutput>(),
                properties = new Dictionary<string, object> { { "Node name for S&R", "SaveAudio" } },
                widgets_values = new object[] { config.FilenamePrefix } // Only filename_prefix as in the example
            };
        }
    }
}