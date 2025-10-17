namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// ComfyUI queue status information
    /// </summary>
    public class ComfyUIQueueStatus
    {
        /// <summary>
        /// Currently executing workflows
        /// </summary>
        public List<ComfyUIQueueItem> exec { get; set; } = new();
        
        /// <summary>
        /// Queued workflows waiting for execution
        /// </summary>
        public List<ComfyUIQueueItem> queue { get; set; } = new();
    }
    
    /// <summary>
    /// Individual queue item in ComfyUI
    /// </summary>
    public class ComfyUIQueueItem
    {
        /// <summary>
        /// Unique prompt ID
        /// </summary>
        public string prompt_id { get; set; } = string.Empty;
        
        /// <summary>
        /// Execution number
        /// </summary>
        public int number { get; set; }
        
        /// <summary>
        /// Workflow prompt data
        /// </summary>
        public object prompt { get; set; } = new object();
    }
}