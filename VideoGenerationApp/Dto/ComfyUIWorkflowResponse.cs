using System.Text.Json;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Response from ComfyUI workflow execution
    /// </summary>
    public class ComfyUIWorkflowResponse
    {
        /// <summary>
        /// Unique prompt ID for tracking the workflow execution
        /// </summary>
        public string prompt_id { get; set; } = string.Empty;
        
        /// <summary>
        /// Number indicating execution order
        /// </summary>
        public int number { get; set; }
        
        /// <summary>
        /// Error information if workflow validation failed
        /// </summary>
        public ComfyUIError? error { get; set; }
        
        /// <summary>
        /// Node-specific errors (flexible object structure)
        /// </summary>
        public JsonElement? node_errors { get; set; }
    }

    /// <summary>
    /// Error details from ComfyUI
    /// </summary>
    public class ComfyUIError
    {
        public string type { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string details { get; set; } = string.Empty;
        public JsonElement? extra_info { get; set; }
    }
}