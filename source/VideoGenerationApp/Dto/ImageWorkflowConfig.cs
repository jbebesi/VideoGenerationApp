namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Configuration parameters for image generation workflow
    /// </summary>
    public class ImageWorkflowConfig
    {
        #region Model Set Selection
        
        /// <summary>
        /// Predefined model sets for image generation
        /// Each set contains compatible models that work together
        /// </summary>
        public string ModelSet { get; set; } = "QWEN_IMAGE_FP8";
        
        /// <summary>
        /// Available model sets with their configurations
        /// </summary>
        public static readonly Dictionary<string, ImageModelSetConfig> ModelSets = new()
        {
            ["QWEN_IMAGE_FP8"] = new()
            {
                DisplayName = "Qwen-Image FP8 (High Quality)",
                CheckpointName = "qwen_image_fp8_e4m3fn.safetensors",
                UNetModel = "qwen_image_fp8_e4m3fn.safetensors",
                CLIPModel = "qwen_2.5_vl_7b_fp8_scaled.safetensors",
                CLIPType = "qwen_image",
                VAEModel = "qwen_image_vae.safetensors",
                LoRAModel = null,
                LoRAStrength = 0.0f,
                ModelSamplingShift = 3.1f,
                DefaultSteps = 20,
                DefaultCFG = 2.5f,
                RecommendedSampler = "euler",
                RecommendedScheduler = "simple"
            },
            ["QWEN_IMAGE_FP8_LIGHTNING"] = new()
            {
                DisplayName = "Qwen-Image FP8 Lightning (Fast)",
                CheckpointName = "qwen_image_fp8_e4m3fn.safetensors",
                UNetModel = "qwen_image_fp8_e4m3fn.safetensors",
                CLIPModel = "qwen_2.5_vl_7b_fp8_scaled.safetensors",
                CLIPType = "qwen_image",
                VAEModel = "qwen_image_vae.safetensors",
                LoRAModel = "Qwen-Image-Lightning-8steps-V1.0.safetensors",
                LoRAStrength = 1.0f,
                ModelSamplingShift = 3.1f,
                DefaultSteps = 8,
                DefaultCFG = 1.0f,
                RecommendedSampler = "euler",
                RecommendedScheduler = "simple"
            },
            ["SD_1_5"] = new()
            {
                DisplayName = "Stable Diffusion 1.5 (Classic)",
                CheckpointName = "v1-5-pruned-emaonly.safetensors",
                UNetModel = "v1-5-pruned-emaonly.safetensors",
                CLIPModel = null, // SD 1.5 uses checkpoint's built-in CLIP
                CLIPType = "sd1",
                VAEModel = "vae-ft-mse-840000-ema-pruned.safetensors",
                LoRAModel = null,
                LoRAStrength = 0.0f,
                ModelSamplingShift = 0.0f, // SD 1.5 doesn't use shift
                DefaultSteps = 20,
                DefaultCFG = 7.0f,
                RecommendedSampler = "euler_ancestral",
                RecommendedScheduler = "normal"
            },
            ["SDXL_TURBO"] = new()
            {
                DisplayName = "SDXL Turbo (Ultra Fast)",
                CheckpointName = "sd_xl_turbo_1.0_fp16.safetensors",
                UNetModel = "sd_xl_turbo_1.0_fp16.safetensors",
                CLIPModel = null, // SDXL uses checkpoint's built-in CLIP
                CLIPType = "sdxl",
                VAEModel = "sdxl_vae.safetensors",
                LoRAModel = null,
                LoRAStrength = 0.0f,
                ModelSamplingShift = 0.0f, // SDXL doesn't use shift
                DefaultSteps = 4, // Turbo is optimized for 1-4 steps
                DefaultCFG = 1.0f, // Turbo works best with low CFG
                RecommendedSampler = "euler_ancestral",
                RecommendedScheduler = "normal"
            }
        };

        #endregion

        // Prompt settings
        public string PositivePrompt { get; set; } = "beautiful landscape, high quality, detailed";
        public string NegativePrompt { get; set; } = "ugly, blurry, low quality";
        
        // Model settings - now derived from ModelSet
        public string CheckpointName 
        { 
            get => GetCurrentModelSet().CheckpointName;
            set => _checkpointName = value; // Allow override if needed
        }
        private string? _checkpointName;
        
        // Image dimensions
        public int Width { get; set; } = 1024;
        public int Height { get; set; } = 1024;
        
        // Sampling settings
        public long Seed { get; set; } = -1; // -1 for random
        public int Steps { get; set; } = 20;
        public float CFGScale { get; set; } = 7.0f;
        public string SamplerName { get; set; } = "euler";
        public string Scheduler { get; set; } = "normal";
        public float Denoise { get; set; } = 1.0f;
        
        // Batch settings
        public int BatchSize { get; set; } = 1;
        
        // Output settings
        public string OutputFilename { get; set; } = "image/ComfyUI";
        public string OutputFormat { get; set; } = "png"; // png, jpg, webp

        /// <summary>
        /// Get the current model set configuration
        /// </summary>
        public ImageModelSetConfig GetCurrentModelSet()
        {
            return ModelSets.TryGetValue(ModelSet, out var config) ? config : ModelSets["QWEN_IMAGE_FP8"];
        }

        /// <summary>
        /// Apply model set defaults to this configuration
        /// </summary>
        public void ApplyModelSetDefaults()
        {
            var modelSet = GetCurrentModelSet();
            Steps = modelSet.DefaultSteps;
            CFGScale = modelSet.DefaultCFG;
            SamplerName = modelSet.RecommendedSampler;
            Scheduler = modelSet.RecommendedScheduler;
        }
    }

    /// <summary>
    /// Configuration for a specific image model set
    /// </summary>
    public class ImageModelSetConfig
    {
        public string DisplayName { get; set; } = string.Empty;
        public string CheckpointName { get; set; } = string.Empty;
        public string UNetModel { get; set; } = string.Empty;
        public string? CLIPModel { get; set; } // Nullable for SD 1.5/SDXL which use built-in CLIP
        public string CLIPType { get; set; } = string.Empty;
        public string VAEModel { get; set; } = string.Empty;
        public string? LoRAModel { get; set; }
        public float LoRAStrength { get; set; }
        public float ModelSamplingShift { get; set; }
        public int DefaultSteps { get; set; }
        public float DefaultCFG { get; set; }
        public string RecommendedSampler { get; set; } = string.Empty;
        public string RecommendedScheduler { get; set; } = string.Empty;
    }
}
