using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Interface for generation services that handle specific types of content generation
    /// </summary>
    public interface IGenerationService<TConfig> where TConfig : class
    {
        /// <summary>
        /// The type of generation this service handles
        /// </summary>
        GenerationType Type { get; }
        
        /// <summary>
        /// Submit a generation task with the specified configuration
        /// </summary>
        /// <param name="task">The generation task to submit</param>
        /// <param name="config">The configuration for this generation type</param>
        /// <returns>The prompt ID from ComfyUI or null if submission failed</returns>
        Task<string?> SubmitTaskAsync(GenerationTask task, TConfig config);
        
        /// <summary>
        /// Check if a task has completed and download the generated file
        /// </summary>
        /// <param name="task">The task to check</param>
        /// <returns>The file path if completed, null otherwise</returns>
        Task<string?> CheckTaskCompletionAsync(GenerationTask task);
        
        /// <summary>
        /// Cancel a task that's currently in progress
        /// </summary>
        /// <param name="promptId">The ComfyUI prompt ID to cancel</param>
        /// <returns>True if cancellation was successful</returns>
        Task<bool> CancelTaskAsync(string promptId);
        
        /// <summary>
        /// Get the queue status for this generation type
        /// </summary>
        /// <returns>The ComfyUI queue status</returns>
        Task<ComfyUIQueueStatus?> GetQueueStatusAsync();
        
        /// <summary>
        /// Get available models for this generation type
        /// </summary>
        /// <returns>List of available model names</returns>
        Task<List<string>> GetModelsAsync();
        
        /// <summary>
        /// Create a new generation task from the provided configuration
        /// </summary>
        /// <param name="name">The task name</param>
        /// <param name="config">The generation configuration</param>
        /// <param name="notes">Optional notes for the task</param>
        /// <returns>A new GenerationTask</returns>
        GenerationTask CreateTask(string name, TConfig config, string? notes = null);
        
        /// <summary>
        /// Get additional model types supported by this service
        /// </summary>
        Task<List<string>> GetCLIPModelsAsync();
        Task<List<string>> GetVAEModelsAsync();
        Task<List<string>> GetUNETModelsAsync();
        Task<List<string>> GetAudioEncoderModelsAsync();
        Task<List<string>> GetLoRAModelsAsync();
    }
}