namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents video-specific instructions for generation
    /// </summary>
    public class VideoSection
    {
        /// <summary>
        /// List of keywords or phrases to enhance visual quality (e.g., ["high resolution", "vibrant colors", "cinematic lighting"])
        /// </summary>
        public string positive_prompt { get; set; } = string.Empty;

        /// <summary>
        /// List of keywords or phrases to avoid in the visuals (e.g., ["blurry", "dark", "low quality"])
        /// </summary>
        public string negative_prompt { get; set; } = string.Empty;
    }
}