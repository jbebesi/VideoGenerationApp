namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Configuration parameters for image generation workflow
    /// </summary>
    public class ImageWorkflowConfig
    {
        // Prompt settings
        public string PositivePrompt { get; set; } = "beautiful landscape, high quality, detailed";
        public string NegativePrompt { get; set; } = "ugly, blurry, low quality";
        
        // Model settings
        public string CheckpointName { get; set; } = "sd_xl_base_1.0.safetensors";
        
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
    }
}
