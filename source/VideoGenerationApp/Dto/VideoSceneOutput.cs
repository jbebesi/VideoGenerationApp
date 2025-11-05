namespace VideoGenerationApp.Dto
{
    public class VideoSceneOutput
    {
        public string narrative { get; set; } = string.Empty;
        public string tone { get; set; } = string.Empty;
        public string emotion { get; set; } = string.Empty;
        public string voice_style { get; set; } = string.Empty;
        public string visual_description { get; set; } = string.Empty;
        public List<string> video_actions { get; set; } = new();
        
        /// <summary>
        /// Audio-specific instructions and preferences for the video scene
        /// </summary>
        public AudioSection audio { get; set; } = new();
        
        /// <summary>
        /// Video-specific instructions for generation
        /// </summary>
        public VideoSection video { get; set; } = new();
        
        /// <summary>
        /// Image-specific instructions for generation
        /// </summary>
        public ImageSection image { get; set; } = new();
        
        /// <summary>
        /// Path to the generated audio file (relative to wwwroot or absolute path)
        /// </summary>
        public string audio_file_path { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when the audio was generated
        /// </summary>
        public DateTimeOffset? audio_generated_at { get; set; }
    }
}