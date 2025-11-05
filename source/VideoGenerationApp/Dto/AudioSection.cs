namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents audio-specific instructions and preferences for a video scene
    /// </summary>
    public class AudioSection
    {
        /// <summary>
        /// Text of any spoken words or dialogue
        /// </summary>
        public string lyrics { get; set; } = string.Empty;

        /// <summary>
        /// List of keywords or phrases to enhance audio quality (e.g., ["clear voice", "background music", "natural sound"])
        /// </summary>
        public List<string> tags { get; set; } = new();
    }
}