using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// ViewModel for video generation parameters - used by UI components
    /// This is transformed into VideoWorkflowConfig via factory methods
    /// </summary>
    public class VideoWorkflowWrapper
    {
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
        /// Checkpoint/model name
        /// </summary>
        public string CheckpointName { get; set; } = string.Empty;

        /// <summary>
        /// Random seed for generation
        /// </summary>
        public int Seed { get; set; } = -1;

        /// <summary>
        /// Number of sampling steps
        /// </summary>
        public int Steps { get; set; } = 20;

        /// <summary>
        /// CFG Scale for prompt adherence
        /// </summary>
        public float CFGScale { get; set; } = 7.0f;

        /// <summary>
        /// Sampler name
        /// </summary>
        public string SamplerName { get; set; } = "euler";

        /// <summary>
        /// Scheduler name
        /// </summary>
        public string Scheduler { get; set; } = "normal";

        /// <summary>
        /// Denoise strength
        /// </summary>
        public float Denoise { get; set; } = 1.0f;

        #endregion

        #region Video Dimensions and Timing

        /// <summary>
        /// Video width in pixels
        /// </summary>
        public int Width { get; set; } = 512;

        /// <summary>
        /// Video height in pixels
        /// </summary>
        public int Height { get; set; } = 512;

        /// <summary>
        /// Number of frames to generate
        /// </summary>
        public int FrameCount { get; set; } = 25;

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; } = 5;

        /// <summary>
        /// Frames per second
        /// </summary>
        public int Fps { get; set; } = 8;

        #endregion

        #region Motion and Animation

        /// <summary>
        /// Motion bucket ID for video generation
        /// </summary>
        public int MotionBucketId { get; set; } = 127;

        /// <summary>
        /// Augmentation level for video generation
        /// </summary>
        public float AugmentationLevel { get; set; } = 0.0f;

        /// <summary>
        /// Animation style
        /// </summary>
        public string AnimationStyle { get; set; } = "default";

        /// <summary>
        /// Motion intensity (0.0 to 1.0)
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
        /// Output filename
        /// </summary>
        public string OutputFilename { get; set; } = string.Empty;

        /// <summary>
        /// Output format (mp4, avi, etc.)
        /// </summary>
        public string OutputFormat { get; set; } = "mp4";

        /// <summary>
        /// Quality setting
        /// </summary>
        public float Quality { get; set; } = 1.0f;

        #endregion

        #region Utility Methods

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
                TextPrompt = this.TextPrompt,
                NegativePrompt = this.NegativePrompt,
                CheckpointName = this.CheckpointName,
                Seed = this.Seed,
                Steps = this.Steps,
                CFGScale = this.CFGScale,
                SamplerName = this.SamplerName,
                Scheduler = this.Scheduler,
                Denoise = this.Denoise,
                Width = this.Width,
                Height = this.Height,
                FrameCount = this.FrameCount,
                DurationSeconds = this.DurationSeconds,
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
                Quality = this.Quality
            };
        }

        #endregion
    }
}