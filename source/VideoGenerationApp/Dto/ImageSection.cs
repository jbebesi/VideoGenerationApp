namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents image-specific instructions for generation
    /// </summary>
    public class ImageSection
    {
        /// <summary>
        /// List of keywords or phrases to enhance image quality (e.g., ["sharp focus", "bright colors", "detailed textures"])
        /// </summary>
        public string positive_prompt { get; set; } = string.Empty;

        /// <summary>
        /// List of keywords or phrases to avoid in the images (e.g., ["pixelated", "dull colors", "overexposed"])
        /// </summary>
        public string negative_prompt { get; set; } = string.Empty;
    }
}