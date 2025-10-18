using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Configuration parameters for ACE Step audio generation workflow (singing/speech)
    /// </summary>
    public class AudioWorkflowConfig
    {
        // Checkpoint/Model settings for ACE Step
        public string CheckpointName { get; set; } = "ace_step_v1_3.5b.safetensors";

        // ACE Step specific prompts
        public string Tags { get; set; } = "pop, female voice, catchy melody";
        public string Lyrics { get; set; } = "[verse]\nIn the silence of the night\nStars are shining bright\n[chorus]\nSing with me tonight\nEverything will be alright";
        public float LyricsStrength { get; set; } = 0.99f;

        // Audio settings
        public float AudioDurationSeconds { get; set; } = 120f; // Longer duration for songs
        public int BatchSize { get; set; } = 1;

        // Model configuration
        public float ModelShift { get; set; } = 5.0f; // SD3 sampling shift
        public float TonemapMultiplier { get; set; } = 1.0f; // Reinhard tonemap multiplier

        // Sampling settings
        public long Seed { get; set; } = -1; // -1 for random
        public int Steps { get; set; } = 50;
        public float CFGScale { get; set; } = 5.0f;
        public string SamplerName { get; set; } = "euler";
        public string Scheduler { get; set; } = "simple";
        public float Denoise { get; set; } = 1.0f;

        // Output settings
        public string OutputFilename { get; set; } = "audio/ComfyUI";
        public string OutputFormat { get; set; } = "mp3"; // mp3, flac, opus
        public string AudioQuality { get; set; } = "V0"; // For MP3: V0, V1, V2, etc. For Opus: 128k, 256k, etc.
    }

    /// <summary>
    /// Factory class to create ComfyUI ACE Step audio workflow from configuration
    /// </summary>
    public static class AudioWorkflowFactory
    {
        public static ComfyUIAudioWorkflow CreateWorkflow(AudioWorkflowConfig config)
        {
            var workflow = new ComfyUIAudioWorkflow
            {
                id = Guid.NewGuid().ToString(),
                revision = 0,
                last_node_id = 73,
                last_link_id = 137,
                nodes = new List<ComfyUINode>
                {
                    CreateCheckpointLoaderNode(config),
                    CreateEmptyAceStepLatentAudioNode(config),
                    CreateTextEncodeAceStepAudioNode(config),
                    CreateConditioningZeroOutNode(),
                    CreateLatentOperationTonemapReinhardNode(config),
                    CreateModelSamplingSD3Node(config),
                    CreateLatentApplyOperationCFGNode(),
                    CreateKSamplerNode(config),
                    CreateVAEDecodeAudioNode(),
                    CreateSaveAudioNode(config)
                },
                links = new List<object[]>
                {
                    new object[] { 80, 40, 1, 14, 0, "CLIP" },
                    new object[] { 83, 40, 2, 18, 1, "VAE" },
                    new object[] { 108, 14, 0, 44, 0, "CONDITIONING" },
                    new object[] { 113, 51, 0, 49, 0, "MODEL" },
                    new object[] { 114, 50, 0, 49, 1, "LATENT_OPERATION" },
                    new object[] { 115, 40, 0, 51, 0, "MODEL" },
                    new object[] { 117, 14, 0, 52, 1, "CONDITIONING" },
                    new object[] { 119, 17, 0, 52, 3, "LATENT" },
                    new object[] { 120, 44, 0, 52, 2, "CONDITIONING" },
                    new object[] { 121, 49, 0, 52, 0, "MODEL" },
                    new object[] { 122, 52, 0, 18, 0, "LATENT" },
                    new object[] { 126, 18, 0, 59, 0, "AUDIO" }
                },
                extra = new Dictionary<string, object>(),
                version = "0.4"
            };

            return workflow;
        }

        private static ComfyUINode CreateCheckpointLoaderNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 40,
                type = "CheckpointLoaderSimple",
                pos = new[] { 180, -160 },
                size = new[] { 370, 98 },
                order = 1,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "ckpt_name", type = "COMBO", widget = new ComfyUIWidget { name = "ckpt_name" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 115 } },
                    new() { name = "CLIP", type = "CLIP", links = new List<int> { 80 } },
                    new() { name = "VAE", type = "VAE", links = new List<int> { 83 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "CheckpointLoaderSimple" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.32" }
                },
                widgets_values = new object[] { config.CheckpointName },
                color = "#322",
                bgcolor = "#533"
            };
        }

        private static ComfyUINode CreateEmptyAceStepLatentAudioNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 17,
                type = "EmptyAceStepLatentAudio",
                pos = new[] { 180, 50 },
                size = new[] { 370, 82 },
                order = 5,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "seconds", type = "FLOAT", widget = new ComfyUIWidget { name = "seconds" } },
                    new() { name = "batch_size", type = "INT", widget = new ComfyUIWidget { name = "batch_size" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 119 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "EmptyAceStepLatentAudio" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.32" }
                },
                widgets_values = new object[] { config.AudioDurationSeconds, config.BatchSize }
            };
        }

        private static ComfyUINode CreateTextEncodeAceStepAudioNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 14,
                type = "TextEncodeAceStepAudio",
                pos = new[] { 590, 120 },
                size = new[] { 340, 500 },
                order = 7,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "clip", type = "CLIP", link = 80 },
                    new() { name = "tags", type = "STRING", widget = new ComfyUIWidget { name = "tags" } },
                    new() { name = "lyrics", type = "STRING", widget = new ComfyUIWidget { name = "lyrics" } },
                    new() { name = "lyrics_strength", type = "FLOAT", widget = new ComfyUIWidget { name = "lyrics_strength" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 108, 117 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "TextEncodeAceStepAudio" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.32" }
                },
                widgets_values = new object[] { config.Tags, config.Lyrics, config.LyricsStrength }
            };
        }

        private static ComfyUINode CreateConditioningZeroOutNode()
        {
            return new ComfyUINode
            {
                id = 44,
                type = "ConditioningZeroOut",
                pos = new[] { 600, 70 },
                size = new[] { 197, 26 },
                flags = new Dictionary<string, object> { { "collapsed", true } },
                order = 10,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "conditioning", type = "CONDITIONING", link = 108 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "CONDITIONING", type = "CONDITIONING", links = new List<int> { 120 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "ConditioningZeroOut" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.32" }
                },
                widgets_values = new object[] { }
            };
        }

        private static ComfyUINode CreateLatentOperationTonemapReinhardNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 50,
                type = "LatentOperationTonemapReinhard",
                pos = new[] { 590, -160 },
                size = new[] { 330, 58 },
                order = 4,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "multiplier", type = "FLOAT", widget = new ComfyUIWidget { name = "multiplier" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT_OPERATION", type = "LATENT_OPERATION", links = new List<int> { 114 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "LatentOperationTonemapReinhard" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.34" }
                },
                widgets_values = new object[] { config.TonemapMultiplier }
            };
        }

        private static ComfyUINode CreateModelSamplingSD3Node(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 51,
                type = "ModelSamplingSD3",
                pos = new[] { 590, -40 },
                size = new[] { 330, 60 },
                order = 6,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 115 },
                    new() { name = "shift", type = "FLOAT", widget = new ComfyUIWidget { name = "shift" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 113 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "ModelSamplingSD3" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.34" }
                },
                widgets_values = new object[] { config.ModelShift }
            };
        }

        private static ComfyUINode CreateLatentApplyOperationCFGNode()
        {
            return new ComfyUINode
            {
                id = 49,
                type = "LatentApplyOperationCFG",
                pos = new[] { 940, -160 },
                size = new[] { 290, 50 },
                order = 9,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 113 },
                    new() { name = "operation", type = "LATENT_OPERATION", link = 114 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "MODEL", type = "MODEL", links = new List<int> { 121 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "LatentApplyOperationCFG" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.34" }
                },
                widgets_values = new object[] { }
            };
        }

        private static ComfyUINode CreateKSamplerNode(AudioWorkflowConfig config)
        {
            return new ComfyUINode
            {
                id = 52,
                type = "KSampler",
                pos = new[] { 940, -40 },
                size = new[] { 290, 262 },
                order = 11,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "model", type = "MODEL", link = 121 },
                    new() { name = "positive", type = "CONDITIONING", link = 117 },
                    new() { name = "negative", type = "CONDITIONING", link = 120 },
                    new() { name = "latent_image", type = "LATENT", link = 119 },
                    new() { name = "seed", type = "INT", widget = new ComfyUIWidget { name = "seed" } },
                    new() { name = "steps", type = "INT", widget = new ComfyUIWidget { name = "steps" } },
                    new() { name = "cfg", type = "FLOAT", widget = new ComfyUIWidget { name = "cfg" } },
                    new() { name = "sampler_name", type = "COMBO", widget = new ComfyUIWidget { name = "sampler_name" } },
                    new() { name = "scheduler", type = "COMBO", widget = new ComfyUIWidget { name = "scheduler" } },
                    new() { name = "denoise", type = "FLOAT", widget = new ComfyUIWidget { name = "denoise" } }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "LATENT", type = "LATENT", links = new List<int> { 122 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "KSampler" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.34" }
                },
                widgets_values = new object[] 
                { 
                    config.Seed == -1 ? Random.Shared.NextInt64(0, long.MaxValue) : config.Seed,
                    config.Seed == -1 ? "randomize" : "fixed",
                    config.Steps,
                    config.CFGScale,
                    config.SamplerName,
                    config.Scheduler,
                    config.Denoise,
                    "false"
                }
            };
        }

        private static ComfyUINode CreateVAEDecodeAudioNode()
        {
            return new ComfyUINode
            {
                id = 18,
                type = "VAEDecodeAudio",
                pos = new[] { 1080, 270 },
                size = new[] { 150, 46 },
                order = 12,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "samples", type = "LATENT", link = 122 },
                    new() { name = "vae", type = "VAE", link = 83 }
                },
                outputs = new List<ComfyUIOutput>
                {
                    new() { name = "AUDIO", type = "AUDIO", links = new List<int> { 126 } }
                },
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", "VAEDecodeAudio" },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.32" }
                },
                widgets_values = new object[] { }
            };
        }

        private static ComfyUINode CreateSaveAudioNode(AudioWorkflowConfig config)
        {
            // Determine the node type and settings based on output format
            string nodeType;
            object[] widgetValues;
            
            switch (config.OutputFormat.ToLowerInvariant())
            {
                case "mp3":
                    nodeType = "SaveAudioMP3";
                    widgetValues = new object[] { config.OutputFilename, config.AudioQuality };
                    break;
                case "flac":
                    nodeType = "SaveAudio"; // FLAC support through SaveAudio node
                    widgetValues = new object[] { config.OutputFilename };
                    break;
                case "wav":
                    nodeType = "SaveAudio"; // WAV support through SaveAudio node  
                    widgetValues = new object[] { config.OutputFilename };
                    break;
                default:
                    // Default to MP3 for unknown formats
                    nodeType = "SaveAudioMP3";
                    widgetValues = new object[] { config.OutputFilename, config.AudioQuality };
                    break;
            }

            return new ComfyUINode
            {
                id = 59,
                type = nodeType,
                pos = new[] { 1260, -160 },
                size = new[] { 610, 136 },
                order = 13,
                mode = 0,
                inputs = new List<ComfyUIInput>
                {
                    new() { name = "audio", type = "AUDIO", link = 126 },
                    new() { name = "filename_prefix", type = "STRING", widget = new ComfyUIWidget { name = "filename_prefix" } }
                }.Union(nodeType == "SaveAudioMP3" 
                    ? new[] { new ComfyUIInput { name = "quality", type = "COMBO", widget = new ComfyUIWidget { name = "quality" } } }
                    : Array.Empty<ComfyUIInput>()
                ).Union(nodeType == "SaveAudioMP3"
                    ? new[] { new ComfyUIInput { name = "audioUI", type = "AUDIO_UI", widget = new ComfyUIWidget { name = "audioUI" } } }
                    : Array.Empty<ComfyUIInput>()
                ).ToList(),
                outputs = new List<ComfyUIOutput>(),
                properties = new Dictionary<string, object> 
                { 
                    { "Node name for S&R", nodeType },
                    { "cnr_id", "comfy-core" },
                    { "ver", "0.3.34" }
                },
                widgets_values = widgetValues
            };
        }
    }
}