using System.ComponentModel.DataAnnotations;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Abstract base class for all generation tasks
    /// </summary>
    public abstract class GenerationTaskBase
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
        /// Type of generation task
        /// </summary>
        public abstract GenerationType Type { get; }
        
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
        /// Path to the generated file (relative to wwwroot)
        /// </summary>
        public string? GeneratedFilePath { get; set; }
        
        /// <summary>
        /// Current position in ComfyUI queue (if known)
        /// </summary>
        public int? QueuePosition { get; set; }
        
        /// <summary>
        /// Additional metadata or notes
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Abstract method to submit this task for processing
        /// </summary>
        public abstract Task<string?> SubmitAsync();
        
        /// <summary>
        /// Abstract method to check if this task has completed
        /// </summary>
        public abstract Task<string?> CheckCompletionAsync();
        
        /// <summary>
        /// Abstract method to cancel this task
        /// </summary>
        public abstract Task<bool> CancelAsync();
        
        /// <summary>
        /// Get the output subfolder for this generation type
        /// </summary>
        protected abstract string OutputSubfolder { get; }
        
        /// <summary>
        /// Get the file prefix for this generation type
        /// </summary>
        protected abstract string FilePrefix { get; }
        
        /// <summary>
        /// Gets a summary of file information for debugging/logging
        /// Virtual method that can be overridden by specific task types
        /// </summary>
        public virtual string GetFileInfoSummary()
        {
            return "No file information available";
        }
    }
}