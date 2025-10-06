using System.Collections.Concurrent;
using VideoGenerationApp.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Service to manage audio generation tasks and queue
    /// </summary>
    public class GenerationQueueService : IHostedService
    {
        private readonly ConcurrentDictionary<string, GenerationTask> _tasks = new();
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<GenerationQueueService> _logger;
        private Timer? _monitorTimer;
        
        public GenerationQueueService(IServiceScopeFactory serviceScopeFactory, ILogger<GenerationQueueService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
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
            
            // Check for completed tasks every 15 seconds (more frequent updates)
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
        /// Adds a new audio generation task to the queue
        /// </summary>
        public virtual async Task<string> QueueGenerationAsync(string name, AudioWorkflowConfig config, string? notes = null)
        {
            var task = new GenerationTask
            {
                Name = name,
                PositivePrompt = $"{config.Tags} - {config.Lyrics?.Substring(0, Math.Min(50, config.Lyrics?.Length ?? 0))}",
                AudioConfig = config,
                Type = GenerationType.Audio,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
            
            _tasks[task.Id] = task;
            _logger.LogInformation("Queued new audio generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Adds a new image generation task to the queue
        /// </summary>
        public virtual async Task<string> QueueImageGenerationAsync(string name, ImageWorkflowConfig config, string? notes = null)
        {
            var task = new GenerationTask
            {
                Name = name,
                PositivePrompt = config.PositivePrompt,
                ImageConfig = config,
                Type = GenerationType.Image,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
            
            _tasks[task.Id] = task;
            _logger.LogInformation("Queued new image generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Adds a new video generation task to the queue
        /// </summary>
        public virtual async Task<string> QueueVideoGenerationAsync(string name, VideoWorkflowConfig config, string? notes = null)
        {
            var task = new GenerationTask
            {
                Name = name,
                PositivePrompt = config.TextPrompt,
                VideoConfig = config,
                Type = GenerationType.Video,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
            
            _tasks[task.Id] = task;
            _logger.LogInformation("Queued new video generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Gets all generation tasks
        /// </summary>
        public virtual IEnumerable<GenerationTask> GetAllTasks()
        {
            return _tasks.Values.OrderByDescending(t => t.CreatedAt);
        }
        
        /// <summary>
        /// Gets all generation tasks asynchronously
        /// </summary>
        public virtual async Task<IEnumerable<GenerationTask>> GetAllTasksAsync()
        {
            return await Task.FromResult(_tasks.Values.OrderByDescending(t => t.CreatedAt));
        }
        
        /// <summary>
        /// Gets a specific task by ID
        /// </summary>
        public virtual GenerationTask? GetTask(string taskId)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }
        
        /// <summary>
        /// Cancels a pending or queued task
        /// </summary>
        public virtual async Task<bool> CancelTaskAsync(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == GenerationStatus.Pending || task.Status == GenerationStatus.Queued || task.Status == GenerationStatus.Processing)
                {
                    // If task has a prompt ID, try to cancel it in ComfyUI
                    if (!string.IsNullOrEmpty(task.PromptId))
                    {
                        try
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var audioService = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                            
                            var cancelled = await audioService.CancelJobAsync(task.PromptId);
                            if (cancelled)
                            {
                                _logger.LogInformation("Successfully cancelled ComfyUI job {PromptId} for task {TaskId}", task.PromptId, task.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to cancel ComfyUI job {PromptId} for task {TaskId}, but will mark task as cancelled locally", task.PromptId, task.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error cancelling ComfyUI job {PromptId} for task {TaskId}, but will mark task as cancelled locally", task.PromptId, task.Id);
                        }
                    }
                    
                    // Update local task status regardless of ComfyUI cancellation success
                    task.Status = GenerationStatus.Cancelled;
                    task.CompletedAt = DateTime.UtcNow;
                    task.ErrorMessage = "Cancelled by user";
                    
                    _logger.LogInformation("Cancelled task: {TaskId} - {Name}", task.Id, task.Name);
                    TaskStatusChanged?.Invoke(task);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Cancels a pending or queued task (synchronous version for backward compatibility)
        /// </summary>
        public virtual bool CancelTask(string taskId)
        {
            return CancelTaskAsync(taskId).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Removes completed, failed, or cancelled tasks
        /// </summary>
        public virtual int ClearCompletedTasks()
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
        /// Submits a task to ComfyUI
        /// </summary>
        private async Task SubmitTaskAsync(GenerationTask task)
        {
            try
            {
                task.Status = GenerationStatus.Queued;
                task.SubmittedAt = DateTime.UtcNow;
                
                using var scope = _serviceScopeFactory.CreateScope();
                
                string? promptId = null;
                
                // Handle different task types
                switch (task.Type)
                {
                    case GenerationType.Audio:
                        if (task.AudioConfig != null)
                        {
                            var audioService = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                            var workflow = AudioWorkflowFactory.CreateWorkflow(task.AudioConfig);
                            var workflowDict = audioService.ConvertWorkflowToComfyUIFormat(workflow);
                            promptId = await audioService.SubmitWorkflowAsync(workflowDict);
                        }
                        break;
                    
                    case GenerationType.Image:
                    case GenerationType.Video:
                        // For now, these will be marked as pending until we implement the services
                        // Image and video generation will be implemented as simple placeholders
                        task.Status = GenerationStatus.Pending;
                        task.ErrorMessage = "Image and video generation support coming soon";
                        _logger.LogWarning("Image/Video generation not yet fully implemented for task {TaskId}", task.Id);
                        TaskStatusChanged?.Invoke(task);
                        return;
                        
                    default:
                        task.Status = GenerationStatus.Failed;
                        task.ErrorMessage = "Unknown task type";
                        _logger.LogError("Unknown task type {Type} for task {TaskId}", task.Type, task.Id);
                        TaskStatusChanged?.Invoke(task);
                        return;
                }
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    task.PromptId = promptId;
                    // Keep status as Queued until ComfyUI actually starts processing
                    _logger.LogInformation("Submitted task {TaskId} to ComfyUI with prompt ID: {PromptId} - Status: Queued", task.Id, promptId);
                }
                else
                {
                    task.Status = GenerationStatus.Failed;
                    task.CompletedAt = DateTime.UtcNow;
                    task.ErrorMessage = "Failed to submit to ComfyUI";
                    _logger.LogError("Failed to submit task {TaskId} to ComfyUI", task.Id);
                }
                
                TaskStatusChanged?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "Error submitting task {TaskId} to ComfyUI", task.Id);
                TaskStatusChanged?.Invoke(task);
            }
        }
        
        /// <summary>
        /// Periodically checks for completed tasks and downloads files
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
                
                _logger.LogDebug("All tasks: {TotalCount}, Active tasks with PromptId: {ActiveCount}", allTasks.Count, activeTasks.Count);
                
                if (allTasks.Any())
                {
                    var statusSummary = allTasks.GroupBy(t => t.Status)
                        .Select(g => $"{g.Key}: {g.Count()}")
                        .ToList();
                    _logger.LogDebug("Task status summary: {StatusSummary}", string.Join(", ", statusSummary));
                    
                    var tasksWithPromptId = allTasks.Where(t => !string.IsNullOrEmpty(t.PromptId)).ToList();
                    _logger.LogDebug("Tasks with PromptId: {TasksWithPromptId}", tasksWithPromptId.Count);
                }
                
                foreach (var task in activeTasks)
                {
                    await CheckTaskCompletionAsync(task);
                }
                
                // Update queue positions
                await UpdateQueuePositionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking completed tasks");
            }
        }
        
        /// <summary>
        /// Checks if a specific task has completed and downloads the file
        /// </summary>
        private async Task CheckTaskCompletionAsync(GenerationTask task)
        {
            try
            {
                if (string.IsNullOrEmpty(task.PromptId)) return;
                
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                
                // Check if the task is still in ComfyUI queue
                var queueStatus = await audioService.GetQueueStatusAsync();
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
                    
                    // Fire event if status changed
                    if (previousStatus != task.Status)
                    {
                        _logger.LogInformation("Firing TaskStatusChanged event for task {TaskId}: {PreviousStatus} -> {NewStatus}", task.Id, previousStatus, task.Status);
                        TaskStatusChanged?.Invoke(task);
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
                    return;
                }
                
                // Task completed, try to download the file
                var filePath = await audioService.GetGeneratedFileAsync(task.PromptId, "audio", "generated");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    task.Status = GenerationStatus.Completed;
                    task.GeneratedFilePath = filePath;
                    task.CompletedAt = DateTime.UtcNow;
                    task.QueuePosition = null;
                    
                    _logger.LogInformation("Task {TaskId} completed successfully: {FilePath}", task.Id, filePath);
                    _logger.LogInformation("Invoking TaskStatusChanged event for completed task {TaskId}", task.Id);
                    TaskStatusChanged?.Invoke(task);
                }
                else
                {
                    // File not ready yet or failed
                    // We'll check again in the next cycle
                    _logger.LogDebug("Task {TaskId} appears completed but file not ready yet", task.Id);
                }
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = ex.Message;
                task.QueuePosition = null;
                
                _logger.LogError(ex, "Error checking completion for task {TaskId}", task.Id);
                TaskStatusChanged?.Invoke(task);
            }
        }
        
        /// <summary>
        /// Updates queue positions for all processing tasks
        /// </summary>
        private async Task UpdateQueuePositionsAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                
                var queueStatus = await audioService.GetQueueStatusAsync();
                if (queueStatus?.queue == null) return;
                
                var processingTasks = _tasks.Values
                    .Where(t => t.Status == GenerationStatus.Processing && !string.IsNullOrEmpty(t.PromptId))
                    .ToList();
                
                foreach (var task in processingTasks)
                {
                    var queueItem = queueStatus.queue.FirstOrDefault(q => q.prompt_id == task.PromptId);
                    if (queueItem != null)
                    {
                        var position = queueStatus.queue.ToList().IndexOf(queueItem) + 1;
                        if (task.QueuePosition != position)
                        {
                            task.QueuePosition = position;
                            TaskStatusChanged?.Invoke(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue positions");
            }
        }
        
        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}