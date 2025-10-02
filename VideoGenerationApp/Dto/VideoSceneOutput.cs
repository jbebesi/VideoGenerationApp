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
    }
}