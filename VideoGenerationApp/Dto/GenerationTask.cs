using System.ComponentModel.DataAnnotations;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a single audio generation task in the queue
    /// </summary>
    public class GenerationTask
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// ComfyUI prompt ID for tracking
        /// </summary>
        public string? PromptId { get; set; }
        
        /// <summary>
        /// User-friendly name/description for the generation
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The positive prompt used for generation
        /// </summary>
        public string PositivePrompt { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the generation task
        /// </summary>
        public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
        
        /// <summary>
        /// When the task was created/queued
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the task was submitted to ComfyUI
        /// </summary>
        public DateTime? SubmittedAt { get; set; }
        
        /// <summary>
        /// When the task was completed (successfully or failed)
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Error message if the task failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Path to the generated audio file (relative to wwwroot)
        /// </summary>
        public string? GeneratedFilePath { get; set; }
        
        /// <summary>
        /// Audio configuration used for this generation
        /// </summary>
        public AudioWorkflowConfig Config { get; set; } = new();
        
        /// <summary>
        /// Current position in ComfyUI queue (if known)
        /// </summary>
        public int? QueuePosition { get; set; }
        
        /// <summary>
        /// Additional metadata or notes
        /// </summary>
        public string? Notes { get; set; }
    }
    
    /// <summary>
    /// Status of a generation task
    /// </summary>
    public enum GenerationStatus
    {
        /// <summary>
        /// Task created but not yet submitted to ComfyUI
        /// </summary>
        Pending,
        
        /// <summary>
        /// Task submitted to ComfyUI and waiting in queue
        /// </summary>
        Queued,
        
        /// <summary>
        /// Task is currently being processed by ComfyUI
        /// </summary>
        Processing,
        
        /// <summary>
        /// Task completed successfully and file is ready
        /// </summary>
        Completed,
        
        /// <summary>
        /// Task failed due to an error
        /// </summary>
        Failed,
        
        /// <summary>
        /// Task was cancelled by the user
        /// </summary>
        Cancelled
    }
}