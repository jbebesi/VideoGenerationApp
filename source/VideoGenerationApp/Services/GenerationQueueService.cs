using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Simplified service to manage generation tasks queue
    /// No longer depends on specific generation services - tasks handle their own lifecycle
    /// </summary>
    public class GenerationQueueService : IHostedService, IGenerationQueueService
    {
        private readonly ConcurrentDictionary<string, GenerationTaskBase> _tasks = new();
        private readonly ILogger<GenerationQueueService> _logger;
        private Timer? _monitorTimer;
        
        public GenerationQueueService(ILogger<GenerationQueueService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Event raised when a task status changes
        /// </summary>
        public event Action<GenerationTask>? TaskStatusChanged;
        
        /// <summary>
        /// Start the hosted service
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Generation Queue Service is starting...");
            
            // Check for completed tasks every 15 seconds
            _monitorTimer = new Timer(CheckCompletedTasks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            
            _logger.LogInformation("Generation Queue Service started successfully");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Stop the hosted service
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Generation Queue Service is stopping...");
            
            _monitorTimer?.Dispose();
            
            _logger.LogInformation("Generation Queue Service stopped");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Adds a task to the queue and starts processing it
        /// </summary>
        public async Task<string> QueueTaskAsync(GenerationTaskBase task)
        {
            _tasks[task.Id] = task;
            
            _logger.LogInformation("Queued new {TaskType} generation task: {TaskId} - {Name}", 
                task.Type, task.Id, task.Name);




            // Submit task for processing
            await SubmitTaskAsync(task);
            
            // Convert to legacy GenerationTask for event compatibility
            var legacyTask = ConvertToLegacyTask(task);
            TaskStatusChanged?.Invoke(legacyTask);
            
            return task.Id;
        }
        
        /// <summary>
        /// Legacy method for audio generation (backward compatibility)
        /// </summary>
        public async Task<string> QueueGenerationAsync(string name, AudioWorkflowConfig config, string? notes = null)
        {
            // This method should no longer be used - generation workflows should create tasks directly
            throw new NotSupportedException("Use QueueTaskAsync with AudioGenerationTask instead");
        }
        
        /// <summary>
        /// Legacy method for image generation (backward compatibility)
        /// </summary>
        public async Task<string> QueueImageGenerationAsync(string name, ImageWorkflowConfig config, string? notes = null)
        {
            // This method should no longer be used - generation workflows should create tasks directly
            throw new NotSupportedException("Use QueueTaskAsync with ImageGenerationTask instead");
        }
        
        /// <summary>
        /// Legacy method for video generation (backward compatibility)
        /// </summary>
        public async Task<string> QueueVideoGenerationAsync(string name, VideoWorkflowConfig config, string? notes = null)
        {
            // This method should no longer be used - generation workflows should create tasks directly
            throw new NotSupportedException("Use QueueTaskAsync with VideoGenerationTask instead");
        }
        
        /// <summary>
        /// Gets all generation tasks
        /// </summary>
        public IEnumerable<GenerationTask> GetAllTasks()
        {
            return _tasks.Values.Select(ConvertToLegacyTask).OrderByDescending(t => t.CreatedAt);
        }
        
        /// <summary>
        /// Gets all generation tasks asynchronously
        /// </summary>
        public async Task<IEnumerable<GenerationTask>> GetAllTasksAsync()
        {
            return await Task.FromResult(_tasks.Values.Select(ConvertToLegacyTask).OrderByDescending(t => t.CreatedAt));
        }
        
        /// <summary>
        /// Gets a specific task by ID
        /// </summary>
        public GenerationTask? GetTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                return ConvertToLegacyTask(task);
            }
            return null;
        }
        
        /// <summary>
        /// Cancels a pending or queued task
        /// </summary>
        public async Task<bool> CancelTaskAsync(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == GenerationStatus.Pending || 
                    task.Status == GenerationStatus.Queued || 
                    task.Status == GenerationStatus.Processing)
                {
                    // Try to cancel the task using its own cancel method
                    if (!string.IsNullOrEmpty(task.PromptId))
                    {
                        try
                        {
                            var cancelled = await task.CancelAsync();
                            if (cancelled)
                            {
                                _logger.LogInformation("Successfully cancelled ComfyUI job {PromptId} for task {TaskId}", 
                                    task.PromptId, task.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to cancel ComfyUI job {PromptId} for task {TaskId}, but will mark task as cancelled locally", 
                                    task.PromptId, task.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error cancelling ComfyUI job {PromptId} for task {TaskId}, but will mark task as cancelled locally", 
                                task.PromptId, task.Id);
                        }
                    }
                    
                    // Update local task status regardless of ComfyUI cancellation success
                    task.Status = GenerationStatus.Cancelled;
                    task.CompletedAt = DateTime.UtcNow;
                    task.ErrorMessage = "Cancelled by user";
                    
                    _logger.LogInformation("Cancelled task: {TaskId} - {Name}", task.Id, task.Name);
                    
                    var legacyTask = ConvertToLegacyTask(task);
                    TaskStatusChanged?.Invoke(legacyTask);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Cancels a pending or queued task (synchronous version for backward compatibility)
        /// </summary>
        public bool CancelTask(string taskId)
        {
            return CancelTaskAsync(taskId).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Removes completed, failed, or cancelled tasks
        /// </summary>
        public int ClearCompletedTasks()
        {
            var completedTaskIds = _tasks.Values
                .Where(t => t.Status == GenerationStatus.Completed || 
                           t.Status == GenerationStatus.Failed || 
                           t.Status == GenerationStatus.Cancelled)
                .Select(t => t.Id)
                .ToList();
            
            foreach (var taskId in completedTaskIds)
            {
                _tasks.TryRemove(taskId, out _);
            }
            
            _logger.LogInformation("Cleared {Count} completed tasks", completedTaskIds.Count);
            return completedTaskIds.Count;
        }
        
        /// <summary>
        /// Submits a task for processing
        /// </summary>
        private async Task SubmitTaskAsync(GenerationTaskBase task)
        {
            _logger.LogInformation("Starting submission of task {TaskId} ({TaskName}) - Type: {TaskType}", 
                task.Id, task.Name, task.Type);
            
            try
            {
                task.Status = GenerationStatus.Queued;
                task.SubmittedAt = DateTime.UtcNow;
                
                var promptId = await task.SubmitAsync();
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    task.PromptId = promptId;
                    _logger.LogInformation("✓ Successfully submitted {TaskType} task {TaskId} ({TaskName}) to ComfyUI with prompt ID: {PromptId}", 
                        task.Type, task.Id, task.Name, promptId);
                }
                else
                {
                    task.Status = GenerationStatus.Failed;
                    task.CompletedAt = DateTime.UtcNow;
                    task.ErrorMessage = $"Failed to submit {task.Type} workflow to ComfyUI - no prompt ID received.";
                    _logger.LogError("✗ Failed to submit {TaskType} task {TaskId} ({TaskName}) to ComfyUI. No prompt ID received.", 
                        task.Type, task.Id, task.Name);
                }
                
                var legacyTask = ConvertToLegacyTask(task);
                TaskStatusChanged?.Invoke(legacyTask);
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = $"{task.Type} generation failed: {ex.Message}";
                
                _logger.LogError(ex, "✗ Error submitting {TaskType} task {TaskId} ({TaskName}) to ComfyUI. Error: {ErrorMessage}", 
                    task.Type, task.Id, task.Name, ex.Message);
                
                var legacyTask = ConvertToLegacyTask(task);
                TaskStatusChanged?.Invoke(legacyTask);
            }
        }

        /// <summary>
        /// Periodically checks for completed tasks
        /// </summary>
        private async void CheckCompletedTasks(object? state)
        {
            try
            {
                var allTasks = _tasks.Values.ToList();
                var activeTasks = allTasks
                    .Where(t => (t.Status == GenerationStatus.Queued || t.Status == GenerationStatus.Processing) 
                               && !string.IsNullOrEmpty(t.PromptId))
                    .ToList();
                
                // Early exit if no active tasks
                if (activeTasks.Count == 0)
                {
                    return;
                }
                
                _logger.LogDebug("Checking {ActiveCount} active tasks for completion (Total tasks in queue: {TotalCount})", 
                    activeTasks.Count, allTasks.Count);
                
                foreach (var task in activeTasks)
                {
                    _logger.LogDebug("→ Checking {TaskType} task {TaskId} ({TaskName}) - Status: {Status}, PromptId: {PromptId}", 
                        task.Type, task.Id, task.Name, task.Status, task.PromptId);
                    await CheckTaskCompletionAsync(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking completed tasks");
            }
        }
        
        /// <summary>
        /// Checks if a specific task has completed
        /// </summary>
        private async Task CheckTaskCompletionAsync(GenerationTaskBase task)
        {
            try
            {
                var filePath = await task.CheckCompletionAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    _logger.LogInformation("Task {TaskId} ({TaskName}) completed successfully: {FilePath}", 
                        task.Id, task.Name, filePath);
                    
                    var legacyTask = ConvertToLegacyTask(task);
                    TaskStatusChanged?.Invoke(legacyTask);
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
                
                var legacyTask = ConvertToLegacyTask(task);
                TaskStatusChanged?.Invoke(legacyTask);
            }
        }
        
        /// <summary>
        /// Convert new task format to legacy GenerationTask for backward compatibility
        /// </summary>
        private GenerationTask ConvertToLegacyTask(GenerationTaskBase task)
        {
            var legacyTask = new GenerationTask
            {
                Id = task.Id,
                PromptId = task.PromptId,
                Name = task.Name,
                PositivePrompt = task.PositivePrompt,
                Type = task.Type,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                SubmittedAt = task.SubmittedAt,
                CompletedAt = task.CompletedAt,
                ErrorMessage = task.ErrorMessage,
                GeneratedFilePath = task.GeneratedFilePath,
                QueuePosition = task.QueuePosition,
                Notes = task.Notes
            };
            
            // Set type-specific config
            switch (task.Type)
            {
                case GenerationType.Audio when task is AudioGenerationTask audioTask:
                    legacyTask.AudioConfig = audioTask.Config;
                    break;
                case GenerationType.Image when task is ImageGenerationTask imageTask:
                    legacyTask.ImageConfig = imageTask.Config;
                    break;
                case GenerationType.Video when task is VideoGenerationTask videoTask:
                    // No legacy VideoConfig anymore; video workflow is built dynamically
                    break;
            }
            
            return legacyTask;
        }

        // Legacy model accessor methods - these should be moved to dedicated model services
        public async Task<List<string>> GetAudioModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }
        
        public async Task<List<string>> GetImageModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }
        
        public async Task<List<string>> GetVideoModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }

        public async Task<List<string>> GetCLIPModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }

        public async Task<List<string>> GetVAEModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }

        public async Task<List<string>> GetUNETModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }

        public async Task<List<string>> GetAudioEncoderModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }

        public async Task<List<string>> GetLoRAModelsAsync()
        {
            // TODO: Move to dedicated model service
            return new List<string>();
        }
        
        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}