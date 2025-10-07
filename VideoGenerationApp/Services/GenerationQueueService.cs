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
            _logger.LogInformation("Starting submission of task {TaskId} ({TaskName}) - Type: {TaskType}", 
                task.Id, task.Name, task.Type);
            
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
                            _logger.LogDebug("Preparing audio workflow for task {TaskId}", task.Id);
                            var audioService = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                            var workflow = AudioWorkflowFactory.CreateWorkflow(task.AudioConfig);
                            var workflowDict = audioService.ConvertWorkflowToComfyUIFormat(workflow);
                            _logger.LogDebug("Submitting audio workflow to ComfyUI for task {TaskId}", task.Id);
                            promptId = await audioService.SubmitWorkflowAsync(workflowDict);
                            
                            if (!string.IsNullOrEmpty(promptId))
                            {
                                _logger.LogInformation("Audio workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                                    task.Id, promptId);
                            }
                        }
                        else
                        {
                            _logger.LogError("Audio task {TaskId} has no AudioConfig", task.Id);
                        }
                        break;
                    
                    case GenerationType.Image:
                        if (task.ImageConfig != null)
                        {
                            _logger.LogDebug("Preparing image workflow for task {TaskId}", task.Id);
                            var imageService = scope.ServiceProvider.GetRequiredService<ComfyUIImageService>();
                            var workflow = ImageWorkflowFactory.CreateWorkflow(task.ImageConfig);
                            var workflowDict = imageService.ConvertWorkflowToComfyUIFormat(workflow);
                            
                            // Log the workflow being sent
                            var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflowDict, 
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            _logger.LogDebug("Image workflow JSON for task {TaskId}: {WorkflowJson}", task.Id, workflowJson);
                            
                            _logger.LogDebug("Submitting image workflow to ComfyUI for task {TaskId}", task.Id);
                            promptId = await imageService.SubmitWorkflowAsync(workflowDict);
                            
                            // Log the response
                            if (!string.IsNullOrEmpty(promptId))
                            {
                                _logger.LogInformation("Image workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                                    task.Id, promptId);
                            }
                        }
                        else
                        {
                            _logger.LogError("Image task {TaskId} has no ImageConfig", task.Id);
                        }
                        break;
                    
                    case GenerationType.Video:
                        // For now, video will be marked as pending until we implement the service
                        task.Status = GenerationStatus.Pending;
                        task.ErrorMessage = "Video generation support coming soon";
                        _logger.LogWarning("Video generation not yet fully implemented for task {TaskId}", task.Id);
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
                    _logger.LogInformation("Submitted task {TaskId} ({TaskName}) to ComfyUI with prompt ID: {PromptId} - Status: Queued", 
                        task.Id, task.Name, promptId);
                }
                else
                {
                    task.Status = GenerationStatus.Failed;
                    task.CompletedAt = DateTime.UtcNow;
                    task.ErrorMessage = "Failed to submit to ComfyUI - no prompt ID received";
                    _logger.LogError("Failed to submit task {TaskId} ({TaskName}) to ComfyUI - Type: {TaskType}, No prompt ID received. This may indicate ComfyUI is not running or the workflow is invalid.", 
                        task.Id, task.Name, task.Type);
                }
                
                TaskStatusChanged?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "Error submitting task {TaskId} ({TaskName}) to ComfyUI - Type: {TaskType}. Error: {ErrorMessage}", 
                    task.Id, task.Name, task.Type, ex.Message);
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
                
                // Early exit if no active tasks - reduces unnecessary logging and HTTP calls
                if (activeTasks.Count == 0)
                {
                    // Only log if there are any tasks at all (to show failed tasks exist)
                    if (allTasks.Count > 0)
                    {
                        var statusSummary = allTasks.GroupBy(t => t.Status)
                            .Select(g => $"{g.Key}: {g.Count()}")
                            .ToList();
                        _logger.LogDebug("No active tasks to check. Task status summary: {StatusSummary}", string.Join(", ", statusSummary));
                        
                        // Log details about failed tasks for debugging
                        var failedTasks = allTasks.Where(t => t.Status == GenerationStatus.Failed).ToList();
                        foreach (var failedTask in failedTasks)
                        {
                            _logger.LogDebug("Failed task {TaskId} ({TaskName}): {ErrorMessage}, PromptId: {PromptId}", 
                                failedTask.Id, failedTask.Name, failedTask.ErrorMessage ?? "No error message", 
                                string.IsNullOrEmpty(failedTask.PromptId) ? "None" : failedTask.PromptId);
                        }
                    }
                    return;
                }
                
                _logger.LogDebug("Checking {ActiveCount} active tasks (Total tasks: {TotalCount})", activeTasks.Count, allTasks.Count);
                
                if (allTasks.Any())
                {
                    var statusSummary = allTasks.GroupBy(t => t.Status)
                        .Select(g => $"{g.Key}: {g.Count()}")
                        .ToList();
                    _logger.LogDebug("Task status summary: {StatusSummary}", string.Join(", ", statusSummary));
                }
                
                foreach (var task in activeTasks)
                {
                    _logger.LogDebug("Checking completion status for task {TaskId} ({TaskName}) - Status: {Status}, PromptId: {PromptId}", 
                        task.Id, task.Name, task.Status, task.PromptId);
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
                if (string.IsNullOrEmpty(task.PromptId))
                {
                    _logger.LogWarning("Task {TaskId} ({TaskName}) has no PromptId - cannot check completion status", task.Id, task.Name);
                    return;
                }
                
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Get the appropriate service based on task type
                ComfyUIServiceBase? service = null;
                string outputSubfolder = "generated";
                string filePrefix = "generated";
                
                switch (task.Type)
                {
                    case GenerationType.Audio:
                        service = scope.ServiceProvider.GetRequiredService<ComfyUIAudioService>();
                        outputSubfolder = "audio";
                        filePrefix = "audio";
                        break;
                    case GenerationType.Image:
                        service = scope.ServiceProvider.GetRequiredService<ComfyUIImageService>();
                        outputSubfolder = "image";
                        filePrefix = "image";
                        break;
                    default:
                        _logger.LogWarning("Unknown task type {Type} for task {TaskId}", task.Type, task.Id);
                        return;
                }
                
                if (service == null) return;
                
                // Check if the task is still in ComfyUI queue
                ComfyUIQueueStatus? queueStatus = null;
                
                // Get queue status based on service type
                if (service is ComfyUIAudioService audioService)
                {
                    queueStatus = await audioService.GetQueueStatusAsync();
                }
                else if (service is ComfyUIImageService imageService)
                {
                    queueStatus = await imageService.GetQueueStatusAsync();
                }
                
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
                _logger.LogDebug("Task {TaskId} ({TaskName}) not in ComfyUI queue - attempting to download generated file", task.Id, task.Name);
                var filePath = await service.GetGeneratedFileAsync(task.PromptId, outputSubfolder, filePrefix);
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    task.Status = GenerationStatus.Completed;
                    task.GeneratedFilePath = filePath;
                    task.CompletedAt = DateTime.UtcNow;
                    task.QueuePosition = null;
                    
                    _logger.LogInformation("Task {TaskId} ({TaskName}) completed successfully: {FilePath}", task.Id, task.Name, filePath);
                    _logger.LogInformation("Invoking TaskStatusChanged event for completed task {TaskId}", task.Id);
                    TaskStatusChanged?.Invoke(task);
                }
                else
                {
                    // File not ready yet or failed
                    // We'll check again in the next cycle
                    _logger.LogWarning("Task {TaskId} ({TaskName}) appears completed (not in queue) but file not ready yet. Will retry in next check.", 
                        task.Id, task.Name);
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