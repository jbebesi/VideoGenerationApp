using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Abstract base class for generation services that provides common functionality
    /// </summary>
    public abstract class GenerationServiceBase<TConfig> : IGenerationService<TConfig> where TConfig : class
    {
        protected readonly IServiceScopeFactory? _serviceScopeFactory;
        protected readonly ILogger _logger;

        protected GenerationServiceBase(IServiceScopeFactory? serviceScopeFactory, ILogger logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// The type of generation this service handles
        /// </summary>
        public abstract GenerationType Type { get; }

        /// <summary>
        /// The output subfolder for generated files
        /// </summary>
        protected abstract string OutputSubfolder { get; }

        /// <summary>
        /// The file prefix for generated files
        /// </summary>
        protected abstract string FilePrefix { get; }

        /// <summary>
        /// Submit a generation task with the specified configuration
        /// </summary>
        public abstract Task<string?> SubmitTaskAsync(GenerationTask task, TConfig config);

        /// <summary>
        /// Create a new generation task from the provided configuration
        /// </summary>
        public abstract GenerationTask CreateTask(string name, TConfig config, string? notes = null);

        /// <summary>
        /// Get available models for this generation type
        /// </summary>
        public abstract Task<List<string>> GetModelsAsync();

        /// <summary>
        /// Get the queue status for this generation type
        /// </summary>
        public abstract Task<ComfyUIQueueStatus?> GetQueueStatusAsync();

        /// <summary>
        /// Cancel a task that's currently in progress
        /// </summary>
        public abstract Task<bool> CancelTaskAsync(string promptId);

        /// <summary>
        /// Get additional model types - default implementations return empty lists unless overridden
        /// </summary>
        public virtual async Task<List<string>> GetCLIPModelsAsync()
        {
            return new List<string>();
        }

        public virtual async Task<List<string>> GetVAEModelsAsync()
        {
            return new List<string>();
        }

        public virtual async Task<List<string>> GetUNETModelsAsync()
        {
            return new List<string>();
        }

        public virtual async Task<List<string>> GetAudioEncoderModelsAsync()
        {
            return new List<string>();
        }

        public virtual async Task<List<string>> GetLoRAModelsAsync()
        {
            return new List<string>();
        }

        /// <summary>
        /// Check if a task has completed and download the generated file
        /// </summary>
        public async Task<string?> CheckTaskCompletionAsync(GenerationTask task)
        {
            try
            {
                if (string.IsNullOrEmpty(task.PromptId))
                {
                    _logger.LogWarning("Task {TaskId} ({TaskName}) has no PromptId - cannot check completion status", task.Id, task.Name);
                    return null;
                }

                var queueStatus = await GetQueueStatusAsync();
                var isInQueue = queueStatus?.queue?.Any(q => q.prompt_id == task.PromptId) == true;
                var isExecuting = queueStatus?.exec?.Any(q => q.prompt_id == task.PromptId) == true;

                _logger.LogDebug("Task {TaskId} queue check - InQueue: {IsInQueue}, Executing: {IsExecuting}, Current Status: {Status}", 
                    task.Id, isInQueue, isExecuting, task.Status);

                if (isInQueue || isExecuting)
                {
                    // Update status based on ComfyUI state
                    var previousStatus = task.Status;

                    if (isExecuting && task.Status != GenerationStatus.Processing)
                    {
                        task.Status = GenerationStatus.Processing;
                        _logger.LogInformation("Task {TaskId} is being executed by ComfyUI - Status changed from {PreviousStatus} to Processing", task.Id, previousStatus);
                    }
                    else if (isInQueue && task.Status != GenerationStatus.Queued)
                    {
                        task.Status = GenerationStatus.Queued;
                        _logger.LogInformation("Task {TaskId} is in ComfyUI queue - Status changed from {PreviousStatus} to Queued", task.Id, previousStatus);
                    }

                    // Update queue position if available
                    if (isInQueue)
                    {
                        var queueItem = queueStatus?.queue?.FirstOrDefault(q => q.prompt_id == task.PromptId);
                        if (queueItem != null && queueStatus?.queue != null)
                        {
                            var position = queueStatus.queue.ToList().IndexOf(queueItem) + 1;
                            task.QueuePosition = position;
                            _logger.LogDebug("Task {TaskId} queue position: {Position}", task.Id, position);
                        }
                    }
                    else
                    {
                        task.QueuePosition = 0; // Currently executing
                        _logger.LogDebug("Task {TaskId} is currently executing (position 0)", task.Id);
                    }
                    return null; // Still in progress
                }

                // Task completed, try to download the file
                _logger.LogDebug("Task {TaskId} ({TaskName}) not in ComfyUI queue - attempting to download generated file", task.Id, task.Name);

                var filePath = await GetGeneratedFileAsync(task.PromptId);
                if (!string.IsNullOrEmpty(filePath))
                {
                    task.Status = GenerationStatus.Completed;
                    task.GeneratedFilePath = filePath;
                    task.CompletedAt = DateTime.UtcNow;
                    task.QueuePosition = null;

                    _logger.LogInformation("Task {TaskId} ({TaskName}) completed successfully: {FilePath}", task.Id, task.Name, filePath);
                    return filePath;
                }
                else
                {
                    // File not ready yet or failed
                    _logger.LogWarning("Task {TaskId} ({TaskName}) appears completed (not in queue) but file not ready yet. Will retry in next check.", 
                        task.Id, task.Name);
                    return null;
                }
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = ex.Message;
                task.QueuePosition = null;

                _logger.LogError(ex, "Error checking completion for task {TaskId} ({TaskName}) - Type: {TaskType}, PromptId: {PromptId}", 
                    task.Id, task.Name, task.Type, task.PromptId);
                throw;
            }
        }

        /// <summary>
        /// Get the generated file from ComfyUI
        /// </summary>
        protected abstract Task<string?> GetGeneratedFileAsync(string promptId);
    }
}