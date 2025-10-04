namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Request object for ComfyUI workflow execution
    /// </summary>
    public class ComfyUIWorkflowRequest
    {
        /// <summary>
        /// The workflow definition in JSON format
        /// </summary>
        public object prompt { get; set; } = new object();
        
        /// <summary>
        /// Optional client ID for tracking requests
        /// </summary>
        public string? client_id { get; set; }
    }
}