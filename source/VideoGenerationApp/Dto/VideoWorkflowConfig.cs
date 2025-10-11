namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Configuration parameters for video generation workflow
    /// </summary>
    public class VideoWorkflowConfig
    {
        // Input references
        public string? AudioFilePath { get; set; }
        public string? ImageFilePath { get; set; }
        public string TextPrompt { get; set; } = string.Empty;
        
        // Video settings
        public float DurationSeconds { get; set; } = 10.0f;
        public int Width { get; set; } = 1024;
        public int Height { get; set; } = 1024;
        public int Fps { get; set; } = 30;
        
        // Animation settings
        public string AnimationStyle { get; set; } = "smooth"; // smooth, dynamic, static
        public float MotionIntensity { get; set; } = 0.5f; // 0.0 to 1.0
        
        // Model settings
        // NOTE: Video generation requires Stable Video Diffusion (SVD) model.
        // Default: svd_xt.safetensors - Download via scripts/install.ps1 or manually from:
        // https://huggingface.co/stabilityai/stable-video-diffusion-img2vid-xt/resolve/main/svd_xt.safetensors
        // See VIDEO_GENERATION_SETUP.md for installation instructions.
        public string CheckpointName { get; set; } = "svd_xt.safetensors";
        public long Seed { get; set; } = -1; // -1 for random
        public int Steps { get; set; } = 20;
        public float CFGScale { get; set; } = 7.0f;
        public float AugmentationLevel { get; set; } = 0.0f; // SVD augmentation level, typically 0.0-0.3
        
        // Output settings
        public string OutputFilename { get; set; } = "video/ComfyUI";
        public string OutputFormat { get; set; } = "mp4"; // mp4, webm, avi
        public int Quality { get; set; } = 90; // 0-100
    }
}
