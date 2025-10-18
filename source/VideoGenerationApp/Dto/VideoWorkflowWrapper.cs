using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// ViewModel for video generation parameters - used by UI components
    /// This is transformed into VideoWorkflowConfig via factory methods
    /// </summary>
    public class VideoWorkflowWrapper
    {
        #region Model Set Selection

        /// <summary>
        /// Selected model set (determines compatible models and default parameters)
        /// </summary>
        public string ModelSet { get; set; } = "WAN_2_2_4Steps";

        #endregion

        #region Basic Generation Parameters

        /// <summary>
        /// Text prompt for video generation
        /// </summary>
        public string TextPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Negative prompt for video generation
        /// </summary>
        public string NegativePrompt { get; set; } = string.Empty;

        /// <summary>
        /// Checkpoint/model name (legacy - now derived from ModelSet)
        /// </summary>
        public string CheckpointName 
        { 
            get => VideoWorkflowConfig.ModelSets.TryGetValue(ModelSet, out var config) 
                ? config.UNetModel 
                : "wan2.2_s2v_14B_fp8_scaled.safetensors";
            set { } // Ignore set operations, use ModelSet instead
        }

        /// <summary>
        /// Random seed for generation (-1 for random)
        /// </summary>
        public long Seed { get; set; } = -1;

        /// <summary>
        /// Number of sampling steps
        /// </summary>
        public int Steps { get; set; } = 4;

        /// <summary>
        /// CFG Scale for prompt adherence
        /// </summary>
        public float CFGScale { get; set; } = 1.0f;

        /// <summary>
        /// Sampler name
        /// </summary>
        public string SamplerName { get; set; } = "uni_pc";

        /// <summary>
        /// Scheduler name
        /// </summary>
        public string Scheduler { get; set; } = "simple";

        /// <summary>
        /// Denoise strength
        /// </summary>
        public float Denoise { get; set; } = 1.0f;

        #endregion

        #region Video Dimensions and Processing

        /// <summary>
        /// Video width in pixels
        /// </summary>
        public int Width { get; set; } = 640;

        /// <summary>
        /// Video height in pixels
        /// </summary>
        public int Height { get; set; } = 640;

        /// <summary>
        /// Chunk length for video processing (frames per segment)
        /// </summary>
        public int ChunkLength { get; set; } = 77;

        /// <summary>
        /// Number of video segments to generate (affects total length)
        /// </summary>
        public int VideoSegments { get; set; } = 2;

        /// <summary>
        /// Batch size (calculated as VideoSegments + 1)
        /// </summary>
        public int BatchSize => VideoSegments + 1;

        /// <summary>
        /// Total estimated frames
        /// </summary>
        public int TotalFrames => ChunkLength * VideoSegments;

        #endregion

        #region Advanced Model Parameters

        /// <summary>
        /// ModelSamplingSD3 shift parameter
        /// </summary>
        public float ModelSamplingShift { get; set; } = 8.0f;

        /// <summary>
        /// LoRA strength (if applicable to selected model set)
        /// </summary>
        public float LoRAStrength { get; set; } = 1.0f;

        #endregion

        #region Legacy Properties (for backward compatibility)

        /// <summary>
        /// Number of frames to generate (legacy - calculated)
        /// </summary>
        public int FrameCount 
        { 
            get => TotalFrames;
            set { } // Calculated property
        }

        /// <summary>
        /// Duration in seconds (legacy - calculated)
        /// </summary>
        public int DurationSeconds 
        { 
            get => (int)(TotalFrames / Fps);
            set { } // Calculated property
        }

        /// <summary>
        /// Frames per second
        /// </summary>
        public int Fps { get; set; } = 16;

        /// <summary>
        /// Motion bucket ID for video generation (legacy)
        /// </summary>
        public int MotionBucketId { get; set; } = 127;

        /// <summary>
        /// Augmentation level for video generation (legacy)
        /// </summary>
        public float AugmentationLevel { get; set; } = 0.0f;

        /// <summary>
        /// Animation style (legacy)
        /// </summary>
        public string AnimationStyle { get; set; } = "default";

        /// <summary>
        /// Motion intensity (0.0 to 1.0) (legacy)
        /// </summary>
        public float MotionIntensity { get; set; } = 0.5f;

        #endregion

        #region File Paths and Output

        /// <summary>
        /// Absolute path to input image file
        /// </summary>
        public string ImageFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Absolute path to input audio file
        /// </summary>
        public string AudioFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the image file (without path)
        /// </summary>
        public string ImageFilename { get; set; } = string.Empty;

        /// <summary>
        /// Output filename prefix
        /// </summary>
        public string OutputFilename { get; set; } = "video/ComfyUI";

        /// <summary>
        /// Output format (mp4, avi, etc.)
        /// </summary>
        public string OutputFormat { get; set; } = "auto";

        /// <summary>
        /// Video codec
        /// </summary>
        public string VideoCodec { get; set; } = "auto";

        /// <summary>
        /// Quality setting (legacy)
        /// </summary>
        public float Quality { get; set; } = 1.0f;

        #endregion

        #region Model Set Management

        /// <summary>
        /// Get available model sets
        /// </summary>
        public static Dictionary<string, ModelSetConfig> GetAvailableModelSets()
        {
            return VideoWorkflowConfig.ModelSets;
        }

        /// <summary>
        /// Get the current model set configuration
        /// </summary>
        public ModelSetConfig GetCurrentModelSet()
        {
            return VideoWorkflowConfig.ModelSets.TryGetValue(ModelSet, out var config) 
                ? config 
                : VideoWorkflowConfig.ModelSets["WAN_2_2_4Steps"];
        }

        /// <summary>
        /// Apply model set defaults to current parameters
        /// </summary>
        public void ApplyModelSetDefaults()
        {
            var modelSet = GetCurrentModelSet();
            Steps = modelSet.DefaultSteps;
            CFGScale = modelSet.DefaultCFG;
            ModelSamplingShift = modelSet.ModelSamplingShift;
            SamplerName = modelSet.RecommendedSampler;
            Scheduler = modelSet.RecommendedScheduler;
            LoRAStrength = modelSet.LoRAStrength;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Convert to VideoWorkflowConfig for backend processing
        /// </summary>
        public VideoWorkflowConfig ToWorkflowConfig()
        {
            return new VideoWorkflowConfig
            {
                ModelSet = this.ModelSet,
                TextPrompt = this.TextPrompt,
                NegativePrompt = this.NegativePrompt,
                Seed = (int)this.Seed,
                Steps = this.Steps,
                CFGScale = this.CFGScale,
                SamplerName = this.SamplerName,
                Scheduler = this.Scheduler,
                Denoise = this.Denoise,
                Width = this.Width,
                Height = this.Height,
                ChunkLength = this.ChunkLength,
                VideoSegments = this.VideoSegments,
                ModelSamplingShift = this.ModelSamplingShift,
                LoRAStrength = this.LoRAStrength,
                ImageFilePath = this.ImageFilePath,
                AudioFilePath = this.AudioFilePath,
                OutputFilename = this.OutputFilename,
                OutputFormat = this.OutputFormat,
                VideoCodec = this.VideoCodec,
                OutputFPS = this.Fps
            };
        }

        /// <summary>
        /// Convert to JSON for debugging/logging
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Create a copy of this ViewModel
        /// </summary>
        public VideoWorkflowWrapper Clone()
        {
            return new VideoWorkflowWrapper
            {
                ModelSet = this.ModelSet,
                TextPrompt = this.TextPrompt,
                NegativePrompt = this.NegativePrompt,
                Seed = this.Seed,
                Steps = this.Steps,
                CFGScale = this.CFGScale,
                SamplerName = this.SamplerName,
                Scheduler = this.Scheduler,
                Denoise = this.Denoise,
                Width = this.Width,
                Height = this.Height,
                ChunkLength = this.ChunkLength,
                VideoSegments = this.VideoSegments,
                ModelSamplingShift = this.ModelSamplingShift,
                LoRAStrength = this.LoRAStrength,
                Fps = this.Fps,
                MotionBucketId = this.MotionBucketId,
                AugmentationLevel = this.AugmentationLevel,
                AnimationStyle = this.AnimationStyle,
                MotionIntensity = this.MotionIntensity,
                ImageFilePath = this.ImageFilePath,
                AudioFilePath = this.AudioFilePath,
                ImageFilename = this.ImageFilename,
                OutputFilename = this.OutputFilename,
                OutputFormat = this.OutputFormat,
                VideoCodec = this.VideoCodec,
                Quality = this.Quality
            };
        }

        #endregion
    }
}