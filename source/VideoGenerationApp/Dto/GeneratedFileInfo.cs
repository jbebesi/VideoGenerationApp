namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Information about a generated file
    /// </summary>
    public class GeneratedFileInfo
    {
        /// <summary>
        /// Display name for the file
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Full path to the file (relative to wwwroot)
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of generated content
        /// </summary>
        public GenerationType Type { get; set; }
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// When the file was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Duration in seconds (for audio/video files)
        /// </summary>
        public float? Duration { get; set; }
        
        /// <summary>
        /// Width in pixels (for image/video files)
        /// </summary>
        public int? Width { get; set; }
        
        /// <summary>
        /// Height in pixels (for image/video files)
        /// </summary>
        public int? Height { get; set; }
        
        /// <summary>
        /// File extension
        /// </summary>
        public string Extension { get; set; } = string.Empty;
        
        /// <summary>
        /// Formatted display text for file selection
        /// </summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string> { Name };
                
                if (Duration.HasValue)
                    parts.Add($"{Duration:F1}s");
                    
                if (Width.HasValue && Height.HasValue)
                    parts.Add($"{Width}x{Height}");
                    
                if (Size > 0)
                {
                    var sizeText = Size switch
                    {
                        < 1024 => $"{Size} B",
                        < 1024 * 1024 => $"{Size / 1024:F1} KB",
                        < 1024 * 1024 * 1024 => $"{Size / (1024 * 1024):F1} MB",
                        _ => $"{Size / (1024 * 1024 * 1024):F1} GB"
                    };
                    parts.Add(sizeText);
                }
                
                return string.Join(" - ", parts);
            }
        }
    }
}