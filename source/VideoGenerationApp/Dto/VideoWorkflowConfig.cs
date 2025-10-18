namespace VideoGenerationApp.Dto
{

    // Workflow builder class
    public class ComfyUIWorkflow
    {
        public Dictionary<string, BaseNode> Nodes { get; set; } = new Dictionary<string, BaseNode>();
        public string ClientId { get; set; } = Guid.NewGuid().ToString();

        public void AddNode(BaseNode node)
        {
            Nodes[node.Id] = node;
        }

        // Validate workflow before sending
        public List<string> ValidateWorkflow()
        {
            var errors = new List<string>();

            foreach (var node in Nodes.Values)
            {
                switch (node.ClassType)
                {
                    case "CLIPLoader":
                        if (!node.Inputs.ContainsKey("clip_name")) errors.Add($"Node {node.Id}: Missing clip_name");
                        if (!node.Inputs.ContainsKey("type")) errors.Add($"Node {node.Id}: Missing type");
                        break;
                    case "UNETLoader":
                        if (!node.Inputs.ContainsKey("unet_name")) errors.Add($"Node {node.Id}: Missing unet_name");
                        break;
                    case "VAELoader":
                        if (!node.Inputs.ContainsKey("vae_name")) errors.Add($"Node {node.Id}: Missing vae_name");
                        break;
                    case "KSampler":
                        var required = new[] { "model", "positive", "negative", "latent_image", "seed", "steps", "cfg" };
                        foreach (var req in required)
                        {
                            if (!node.Inputs.ContainsKey(req)) errors.Add($"Node {node.Id}: Missing {req}");
                        }
                        break;
                    case "SaveVideo":
                        var videoRequired = new[] { "video", "filename_prefix", "codec", "format" };
                        foreach (var req in videoRequired)
                        {
                            if (!node.Inputs.ContainsKey(req)) errors.Add($"Node {node.Id}: Missing {req}");
                        }
                        break;
                }
            }

            return errors;
        }
        public Dictionary<string, object> ToPromptDictionary()
        {
            // Validate before converting
            var errors = ValidateWorkflow();
            if (errors.Any())
            {
                throw new InvalidOperationException($"Workflow validation failed:\n{string.Join("\n", errors)}");
            }
            var prompt = new Dictionary<string, object>();
            foreach (var node in Nodes.Values)
            {
                prompt[node.Id] = new Dictionary<string, object>
                {
                    ["class_type"] = node.ClassType,
                    ["inputs"] = node.Inputs
                };
            }
            return prompt;
        }

        // Helper method to create a complete video generation workflow
        public static ComfyUIWorkflow CreateVideoGenerationWorkflow(
            string positivePrompt,
            string negativePrompt,
            string imagePath,
            string? audioPath = null,
            string unetModel = "wan2.2_s2v_14B_fp8_scaled.safetensors",
            string clipModel = "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
            string clipType = "wan",
            string vaeModel = "wan_2.1_vae.safetensors",
            string audioEncoderModel = "wav2vec2_large_english_fp16.safetensors",
            string? loraModel = "wan2.2_t2v_lightx2v_4steps_lora_v1.1_high_noise.safetensors",
            float loraStrength = 1.0f,
            float modelSamplingShift = 8.0f,
            int width = 640,
            int height = 640,
            int chunkLength = 77,
            int batchSize = 1,
            int seed = 12345,
            bool wasRandomSeed = false, // NEW: Track if original seed was -1
            int steps = 4,
            float cfg = 1.0f,
            int fps = 16,
            string filenamePrefix = "output",
            string samplerName = "euler",
            string scheduler = "normal",
            float denoise = 1.0f,
            string codec = "h264",
            string format = "mp4"
            ) 
        {
            var workflow = new ComfyUIWorkflow();
            
            // ID 1: Load UNET model
            workflow.AddNode(new UNETLoaderNode("1", unetModel));

            // ID 2: Load CLIP model
            workflow.AddNode(new CLIPLoaderNode("2", unetModel, clipModel, clipType));
            
            // ID 3: Load VAE model
            workflow.AddNode(new VAELoaderNode("3", vaeModel));

            // Audio nodes (if audio is provided)
            if (!string.IsNullOrEmpty(audioPath))
            {
                // ID 4: Load Audio Encoder
                workflow.AddNode(new AudioEncoderLoaderNode("4", audioEncoderModel));

                // ID 5: Load Audio file
                workflow.AddNode(new LoadAudioNode("5", audioPath));

                // ID 7: Encode Audio
                workflow.AddNode(new AudioEncoderEncodeNode("7", "4", "5"));
            }

            // ID 6: Load Image file
            workflow.AddNode(new LoadImageNode("6", imagePath));

            // ID 8: Encode positive prompt
            workflow.AddNode(new CLIPTextEncodeNode("8", positivePrompt, "2"));

            // ID 9: Encode negative prompt
            workflow.AddNode(new CLIPTextEncodeNode("9", negativePrompt, "2"));

            string modelNodeId = "1"; // Start with base model
            
            // ID 10: Optional LoRA loader (if LoRA model is provided)
            if (!string.IsNullOrEmpty(loraModel))
            {
                workflow.AddNode(new LoraLoaderModelOnlyNode("10", "1", loraModel, loraStrength));
                modelNodeId = "10"; // Use LoRA-enhanced model
            }

            // ID 11: Model sampling configuration
            workflow.AddNode(new ModelSamplingSD3Node("11", modelNodeId, modelSamplingShift));

            // ID 12: WanSoundImageToVideo node
            string? audioEncoderNodeId = !string.IsNullOrEmpty(audioPath) ? "7" : null;
            if (audioEncoderNodeId != null)
            {
                workflow.AddNode(new WanSoundImageToVideoNode("12", "8", "9", "3", audioEncoderNodeId, "6", width, height, chunkLength, batchSize));
            }
            else
            {
                // Create a version without audio encoder - we need a new constructor for this
                workflow.AddNode(new WanSoundImageToVideoNoAudio("12", "8", "9", "3", "6", width, height, chunkLength, batchSize));
            }

            // ID 13: Main KSampler - use a special constructor that accepts the random seed flag
            workflow.AddNode(new KSamplerNodeWithSeedControl("13", "11", "12", "12", "12", seed, wasRandomSeed, steps, cfg, samplerName, scheduler, denoise));

            // ID 14: VAE Decode
            workflow.AddNode(new VAEDecodeNode("14", "13", "3"));

            // ID 15: Create Video
            if (!string.IsNullOrEmpty(audioPath))
            {
                workflow.AddNode(new CreateVideoNode("15", "14", fps, "5"));
            }
            else
            {
                workflow.AddNode(new CreateVideoNode("15", "14", fps));
            }

            // ID 16: Save Video
            workflow.AddNode(new SaveVideoNode("16", "15", filenamePrefix, codec, format));

            return workflow;
        }
    }
    // Base class for all ComfyUI nodes
    public abstract class BaseNode
    {
        public string Id { get; set; }
        public abstract string ClassType { get; }
        public Dictionary<string, object?> Inputs { get; set; } = new Dictionary<string, object?>();

        protected BaseNode(string id)
        {
            Id = id;
        }

        // Helper method to add node connections
        protected void AddConnection(string inputName, string sourceNodeId, int outputIndex = 0)
        {
            Inputs[inputName] = new object[] { sourceNodeId, outputIndex };
        }

        // Helper method to add simple values
        protected void AddInput(string inputName, object? value)
        {
            Inputs[inputName] = value;
        }
    }


    public class ModelSamplingSD3Node : BaseNode
    {
        public override string ClassType => "ModelSamplingSD3";

        public ModelSamplingSD3Node(string id, string modelNodeId, float shift = 8.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddInput("shift", shift);
        }
    }
    public class LoraLoaderModelOnlyNode : BaseNode
    {
        public override string ClassType => "LoraLoaderModelOnly";

        public LoraLoaderModelOnlyNode(string id, string modelNodeId, string loraName, float strength = 1.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddInput("lora_name", loraName);
            AddInput("strength_model", strength);
        }
    }
    public class AudioEncoderLoaderNode : BaseNode
    {
        public override string ClassType => "AudioEncoderLoader";

        public AudioEncoderLoaderNode(string id, string audioEncoderName = "wav2vec2_large_english_fp16.safetensors") : base(id)
        {
            AddInput("audio_encoder_name", audioEncoderName);
        }
    }

    public class AudioEncoderEncodeNode : BaseNode
    {
        public override string ClassType => "AudioEncoderEncode";

        public AudioEncoderEncodeNode(string id, string audioEncoderNodeId, string audioNodeId) : base(id)
        {
            AddConnection("audio_encoder", audioEncoderNodeId, 0);
            AddConnection("audio", audioNodeId, 0);
        }
    }

    public class WanSoundImageToVideoNode : BaseNode
    {
        public override string ClassType => "WanSoundImageToVideo";

        public WanSoundImageToVideoNode(string id, string positiveNodeId, string negativeNodeId,
                                       string vaeNodeId, string audioEncoderOutputNodeId, string imageNodeId,
                                       int width = 640, int height = 640, int length = 77, int batchSize = 1) : base(id)
        {
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 0);
            AddConnection("vae", vaeNodeId, 0);
            AddConnection("audio_encoder_output", audioEncoderOutputNodeId, 0);
            AddConnection("ref_image", imageNodeId, 0);

            AddInput("width", width);
            AddInput("height", height);
            AddInput("length", length);
            AddInput("batch_size", batchSize);
        }
    }

    public class WanSoundImageToVideoNoAudio : BaseNode
    {
        public override string ClassType => "WanSoundImageToVideo";

        public WanSoundImageToVideoNoAudio(string id, string positiveNodeId, string negativeNodeId,
                                       string vaeNodeId, string imageNodeId,
                                       int width = 640, int height = 640, int length = 77, int batchSize = 1) : base(id)
        {
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 0);
            AddConnection("vae", vaeNodeId, 0);
            AddConnection("ref_image", imageNodeId, 0);

            AddInput("width", width);
            AddInput("height", height);
            AddInput("length", length);
            AddInput("batch_size", batchSize);
        }
    }

    // Audio loading node for speech-to-video generation
    public class LoadAudioNode : BaseNode
    {
        public override string ClassType => "LoadAudio";

        public LoadAudioNode(string id, string audioPath) : base(id)
        {
            AddInput("audio", audioPath);
        }
    }

    // Audio encoding node to convert audio to features
    public class AudioEncodeNode : BaseNode
    {
        public override string ClassType => "AudioEncode";  // Or whatever your ComfyUI uses

        public AudioEncodeNode(string id, string audioNodeId) : base(id)
        {
            AddConnection("audio", audioNodeId, 0);
            AddInput("sample_rate", 16000);
            AddInput("duration", null);  // Auto-detect from audio
        }
    }

    // Multi-modal conditioning node to combine image and video latents
    public class MultiModalConditioningNode : BaseNode
    {
        public override string ClassType => "LatentConcat";  // Or appropriate node for combining latents

        public MultiModalConditioningNode(string id, string imageLatentNodeId, string videoLatentNodeId, string audioNodeId) : base(id)
        {
            AddConnection("samples1", imageLatentNodeId, 0);    // Image latent
            AddConnection("samples2", videoLatentNodeId, 0);    // Video latent frames
            AddConnection("audio_features", audioNodeId, 0);    // Audio features
            AddInput("blend_mode", "interpolate");
            AddInput("strength", 0.7f);  // How much to use image vs video latent
        }
    }

    // Enhanced KSampler that handles all modalities
    public class MultiModalKSamplerNode : BaseNode
    {
        public override string ClassType => "KSampler";

        public MultiModalKSamplerNode(string id, string modelNodeId, string positiveNodeId,
                                    string negativeNodeId, string latentNodeId, string audioNodeId,
                                    int seed = 12345, int steps = 20, float cfg = 7.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 0);
            AddConnection("latent_image", latentNodeId, 0);
            AddConnection("audio_conditioning", audioNodeId, 0);  // Audio conditioning

            AddInput("seed", seed);
            AddInput("steps", steps);
            AddInput("cfg", cfg);
            AddInput("sampler_name", "euler");
            AddInput("scheduler", "normal");
            AddInput("denoise", 1.0f);
        }
    }

    // Fix for video generation - need to generate multiple frames properly
    public class VideoLatentNode : BaseNode
    {
        public override string ClassType => "EmptyLatentImage";  // Create empty latent for video

        public VideoLatentNode(string id, int width = 512, int height = 512, int batchSize = 16) : base(id)
        {
            AddInput("width", width);
            AddInput("height", height);
            AddInput("batch_size", batchSize);  // This creates multiple latent frames
        }
    }

    // New conditioning node to blend image with video latent
    public class ImageConditioningNode : BaseNode
    {
        public override string ClassType => "LatentBlend";  // Or whatever node exists for this

        public ImageConditioningNode(string id, string imageLatentNodeId, string videoLatentNodeId) : base(id)
        {
            AddConnection("samples1", imageLatentNodeId, 0);      // Source image latent
            AddConnection("samples2", videoLatentNodeId, 0);      // Video latent frames
            AddInput("blend_factor", 0.3f);                       // How much to blend
            AddInput("blend_mode", "normal");
        }
    }

    // Enhanced KSampler that supports audio conditioning
    public class AudioVideoKSamplerNode : BaseNode
    {
        public override string ClassType => "KSampler";

        public AudioVideoKSamplerNode(string id, string modelNodeId, string positiveNodeId,
                                    string negativeNodeId, string latentNodeId,
                                    string? audioNodeId = null, int seed = 12345,
                                    int steps = 20, float cfg = 7.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 0);
            AddConnection("latent_image", latentNodeId, 0);

            // Add audio conditioning if provided
            if (!string.IsNullOrEmpty(audioNodeId))
            {
                AddConnection("audio_features", audioNodeId, 0);
            }

            AddInput("seed", seed);
            AddInput("steps", steps);
            AddInput("cfg", cfg);
            AddInput("sampler_name", "euler");
            AddInput("scheduler", "normal");
            AddInput("denoise", 1.0f);
        }
    }

    // Model loading nodes
    public class UNETLoaderNode : BaseNode
    {
        public override string ClassType => "UNETLoader";

        public UNETLoaderNode(string id, string modelName = "wan2.1_t2v_1.3B_fp16.safetensors", string weightType = "default") : base(id)
        {
            AddInput("unet_name", modelName);
            AddInput("weight_dtype", weightType);
        }
    }

    public class CLIPLoaderNode : BaseNode
    {

        private static readonly Dictionary<string, (string clipModel, string type)> ModelCompatibility = new()
    {
        { "wan2.1_t2v_1.3B_fp16.safetensors", ("t5-base.safetensors", "wan") },
        { "wan2.1_vace_1.3B_fp16.safetensors", ("t5-base.safetensors", "wan") },
        { "wan2.2_s2v_14B_fp8_scaled.safetensors", ("umt5_xxl_fp16.safetensors", "wan") },
        { "wan2.2_t2v_high_noise_14B_fp8_scaled.safetensors", ("umt5_xxl_fp16.safetensors", "wan") },
        { "wan2.2_t2v_low_noise_14B_fp8_scaled.safetensors", ("umt5_xxl_fp16.safetensors", "wan") },
        { "qwen_image_fp8_e4m3fn.safetensors", ("qwen_2.5_vl_7b_fp8_scaled.safetensors", "qwen_image") }
    };

        public override string ClassType => "CLIPLoader";

        public CLIPLoaderNode(string id, string unetModelName, string? clipName = null, string? type = null) : base(id)
        {
            if (!string.IsNullOrEmpty(unetModelName) && ModelCompatibility.ContainsKey(unetModelName))
            {
                var (autoClip, autoType) = ModelCompatibility[unetModelName];
                clipName = clipName ?? autoClip;
                type = type ?? autoType;
            }

            AddInput("clip_name", clipName ?? "t5-base.safetensors");
            AddInput("type", type ?? "wan");
        }
    }

    public class VAELoaderNode : BaseNode
    {
        public override string ClassType => "VAELoader";

        public VAELoaderNode(string id, string vaeName = "qwen_image_vae.safetensors") : base(id)
        {
            AddInput("vae_name", vaeName);
        }
    }

    // Text processing nodes
    public class CLIPTextEncodeNode : BaseNode
    {
        public override string ClassType => "CLIPTextEncode";

        public CLIPTextEncodeNode(string id, string text, string clipNodeId) : base(id)
        {
            AddInput("text", text);
            AddConnection("clip", clipNodeId, 0);
        }
    }

    // Image processing nodes
    public class LoadImageNode : BaseNode
    {
        public override string ClassType => "LoadImage";

        public LoadImageNode(string id, string imagePath) : base(id)
        {
            AddInput("image", imagePath);
        }
    }

    public class VAEEncodeNode : BaseNode
    {
        public override string ClassType => "VAEEncode";

        public VAEEncodeNode(string id, string imageNodeId, string vaeNodeId) : base(id)
        {
            AddConnection("pixels", imageNodeId, 0);
            AddConnection("vae", vaeNodeId, 0);
        }
    }

    public class VAEDecodeNode : BaseNode
    {
        public override string ClassType => "VAEDecode";

        public VAEDecodeNode(string id, string samplesNodeId, string vaeNodeId) : base(id)
        {
            AddConnection("samples", samplesNodeId, 0);
            AddConnection("vae", vaeNodeId, 0);
        }
    }


    // Enhanced KSampler for video models
    public class VideoKSamplerNode : BaseNode
    {
        public override string ClassType => "KSampler";

        public VideoKSamplerNode(string id, string modelNodeId, string positiveNodeId, string negativeNodeId,
                               string latentNodeId, int seed = 12345, int steps = 20, float cfg = 7.0f) : base(id) 
        {
            AddConnection("model", modelNodeId, 0);
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 0);
            AddConnection("latent_image", latentNodeId, 0);

            AddInput("seed", seed);
            AddInput("steps", steps);
            AddInput("cfg", cfg);
            AddInput("sampler_name", "euler");
            AddInput("scheduler", "normal");
            AddInput("denoise", 1.0f);
        }
    }


    // Generation nodes
    public class KSamplerNode : BaseNode
    {
        public override string ClassType => "KSampler";

        public KSamplerNode(string id, string modelNodeId, string positiveNodeId, string negativeNodeId,
                           string latentImageNodeId, int seed = 12345, int steps = 4, float cfg = 1.0f,
                           string samplerName = "euler", string scheduler = "normal", float denoise = 1.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 1);
            AddConnection("latent_image", latentImageNodeId, 2);
            AddInput("seed", seed);
            AddInput("control_after_generate", seed == -1 ? "randomize" : "fixed");
            AddInput("steps", steps);
            AddInput("cfg", cfg);
            AddInput("sampler_name", samplerName);
            AddInput("scheduler", scheduler);
            AddInput("denoise", denoise);
        }
    }

    // Enhanced KSampler with proper seed control handling
    public class KSamplerNodeWithSeedControl : BaseNode
    {
        public override string ClassType => "KSampler";

        public KSamplerNodeWithSeedControl(string id, string modelNodeId, string positiveNodeId, string negativeNodeId,
                           string latentImageNodeId, int seed, bool wasRandomSeed, int steps = 4, float cfg = 1.0f,
                           string samplerName = "euler", string scheduler = "normal", float denoise = 1.0f) : base(id)
        {
            AddConnection("model", modelNodeId, 0);
            AddConnection("positive", positiveNodeId, 0);
            AddConnection("negative", negativeNodeId, 1);
            AddConnection("latent_image", latentImageNodeId, 2);
            AddInput("seed", seed);
            AddInput("control_after_generate", wasRandomSeed ? "randomize" : "fixed");
            AddInput("steps", steps);
            AddInput("cfg", cfg);
            AddInput("sampler_name", samplerName);
            AddInput("scheduler", scheduler);
            AddInput("denoise", denoise);
        }
    }

    // Video processing nodes
    public class CreateVideoNode : BaseNode
    {
        public override string ClassType => "CreateVideo";

        public CreateVideoNode(string id, string imagesNodeId, int fps = 16, string? audioNodeId = null) : base(id)
        {
            AddConnection("images", imagesNodeId, 0);
            if (!string.IsNullOrEmpty(audioNodeId))
            {
                AddConnection("audio", audioNodeId, 0);
            }
            AddInput("fps", fps);
        }
    }

    public class SaveVideoNode : BaseNode
    {
        public override string ClassType => "SaveVideo";

        public SaveVideoNode(string id, string videoNodeId, string filenamePrefix = "output",
                            string codec = "h264", string format = "mp4") : base(id)
        {
            AddConnection("video", videoNodeId, 0);
            AddInput("filename_prefix", filenamePrefix);
            AddInput("codec", codec);
            AddInput("format", format);
        }
    }

    // Legacy compatibility class to keep older code/tests compiling
    public class VideoWorkflowConfig
    {
        #region Model Set Selection
        
        /// <summary>
        /// Predefined model sets for video generation
        /// Each set contains compatible models that work together
        /// </summary>
        public string ModelSet { get; set; } = "WAN_2_2_4Steps";
        
        /// <summary>
        /// Available model sets with their configurations
        /// </summary>
        public static readonly Dictionary<string, ModelSetConfig> ModelSets = new()
        {
            ["WAN_2_2_4Steps"] = new()
            {
                DisplayName = "WAN 2.2 (4 Steps Lightning)",
                UNetModel = "wan2.2_s2v_14B_fp8_scaled.safetensors",
                CLIPModel = "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
                CLIPType = "wan",
                VAEModel = "wan_2.1_vae.safetensors",
                AudioEncoderModel = "wav2vec2_large_english_fp16.safetensors",
                LoRAModel = "wan2.2_t2v_lightx2v_4steps_lora_v1.1_high_noise.safetensors",
                LoRAStrength = 1.0f,
                DefaultSteps = 4,
                DefaultCFG = 1.0f,
                ModelSamplingShift = 8.0f,
                RecommendedSampler = "uni_pc",
                RecommendedScheduler = "simple"
            },
            ["WAN_2_2_20Steps"] = new()
            {
                DisplayName = "WAN 2.2 (20 Steps Standard)",
                UNetModel = "wan2.2_s2v_14B_fp8_scaled.safetensors",
                CLIPModel = "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
                CLIPType = "wan",
                VAEModel = "wan_2.1_vae.safetensors",
                AudioEncoderModel = "wav2vec2_large_english_fp16.safetensors",
                LoRAModel = null, // No LoRA for standard workflow
                LoRAStrength = 0.0f,
                DefaultSteps = 20,
                DefaultCFG = 6.0f,
                ModelSamplingShift = 8.0f,
                RecommendedSampler = "uni_pc",
                RecommendedScheduler = "simple"
            },
            ["WAN_2_2_BF16"] = new()
            {
                DisplayName = "WAN 2.2 BF16 (High Quality)",
                UNetModel = "wan2.2_s2v_14B_bf16.safetensors",
                CLIPModel = "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
                CLIPType = "wan",
                VAEModel = "wan_2.1_vae.safetensors",
                AudioEncoderModel = "wav2vec2_large_english_fp16.safetensors",
                LoRAModel = null,
                LoRAStrength = 0.0f,
                DefaultSteps = 20,
                DefaultCFG = 6.0f,
                ModelSamplingShift = 8.0f,
                RecommendedSampler = "uni_pc",
                RecommendedScheduler = "simple"
            }
        };

        #endregion

        public string TextPrompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public string CheckpointName { get; set; } = string.Empty;
        public int Seed { get; set; } = -1;
        public int Steps { get; set; } = 20;
        public float CFGScale { get; set; } = 7.0f;
        public string SamplerName { get; set; } = "euler";
        public string Scheduler { get; set; } = "normal";
        public float Denoise { get; set; } = 1.0f;
        public int Width { get; set; } = 1024;
        public int Height { get; set; } = 1024;
        public int Fps { get; set; } = 30;
        public string AnimationStyle { get; set; } = "smooth";
        public float MotionIntensity { get; set; } = 0.5f;
        public string ImageFilePath { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
        public string OutputFilename { get; set; } = "output";
        public string OutputFormat { get; set; } = "mp4";
        public float Quality { get; set; } = 90;

        // Additional properties needed for video generation
        public int ChunkLength { get; set; } = 77;
        public int VideoSegments { get; set; } = 2;
        public float ModelSamplingShift { get; set; } = 8.0f;
        public float LoRAStrength { get; set; } = 1.0f;
        public string VideoCodec { get; set; } = "h264";
        public float OutputFPS { get; set; } = 16.0f;

        /// <summary>
        /// Get the current model set configuration
        /// </summary>
        public ModelSetConfig GetCurrentModelSet()
        {
            return ModelSets.TryGetValue(ModelSet, out var config) ? config : ModelSets["WAN_2_2_4Steps"];
        }
    }

    /// <summary>
    /// Configuration for a specific model set
    /// </summary>
    public class ModelSetConfig
    {
        public string DisplayName { get; set; } = string.Empty;
        public string UNetModel { get; set; } = string.Empty;
        public string CLIPModel { get; set; } = string.Empty;
        public string CLIPType { get; set; } = string.Empty;
        public string VAEModel { get; set; } = string.Empty;
        public string AudioEncoderModel { get; set; } = string.Empty;
        public string? LoRAModel { get; set; }
        public float LoRAStrength { get; set; }
        public int DefaultSteps { get; set; }
        public float DefaultCFG { get; set; }
        public float ModelSamplingShift { get; set; }
        public string RecommendedSampler { get; set; } = string.Empty;
        public string RecommendedScheduler { get; set; } = string.Empty;
    }
}
