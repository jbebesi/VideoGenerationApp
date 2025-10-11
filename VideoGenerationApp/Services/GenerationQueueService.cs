using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using VideoGenerationApp.Components;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services.Generation;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Service to manage generation tasks and queue
    /// </summary>
    public class GenerationQueueService : IHostedService, IGenerationQueueService
    {
        private readonly ConcurrentDictionary<string, GenerationTask> _tasks = new();
        private readonly ILogger<GenerationQueueService> _logger;
        private readonly IGenerationService<VideoWorkflowConfig> _videoGenerationService;
        private readonly IGenerationService<ImageWorkflowConfig> _imageGenerationService;
        private readonly IGenerationService<AudioWorkflowConfig> _audioGenerationService;
        private Timer? _monitorTimer;
        
        
        

        public GenerationQueueService(
            ILogger<GenerationQueueService> logger,
            IGenerationService<VideoWorkflowConfig> videoGenerationService,
            IGenerationService<ImageWorkflowConfig> imageGenerationService,
            IGenerationService<AudioWorkflowConfig> audioGenerationService)
        {
            _logger = logger;
            _videoGenerationService = videoGenerationService;
            _imageGenerationService = imageGenerationService;
            _audioGenerationService = audioGenerationService;
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
        public async Task<string> QueueGenerationAsync(string name, AudioWorkflowConfig config, string? notes = null)
        {
            var task = _audioGenerationService.CreateTask(name, config, notes);
            _tasks[task.Id] = task;
            
            _logger.LogInformation("Queued new audio generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task, _audioGenerationService, config);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Adds a new image generation task to the queue
        /// </summary>
        public async Task<string> QueueImageGenerationAsync(string name, ImageWorkflowConfig config, string? notes = null)
        {

            var task = _imageGenerationService.CreateTask(name, config, notes);
            _tasks[task.Id] = task;
            
            _logger.LogInformation("Queued new image generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task, _imageGenerationService, config);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Adds a new video generation task to the queue
        /// </summary>
        public async Task<string> QueueVideoGenerationAsync(string name, VideoWorkflowConfig config, string? notes = null)
        {
            var task = _videoGenerationService.CreateTask(name, config, notes);
            _tasks[task.Id] = task;
            
            _logger.LogInformation("Queued new video generation task: {TaskId} - {Name}", task.Id, task.Name);
            
            // Submit to ComfyUI immediately
            await SubmitTaskAsync(task, _videoGenerationService, config);
            
            TaskStatusChanged?.Invoke(task);
            return task.Id;
        }
        
        /// <summary>
        /// Gets all generation tasks
        /// </summary>
        public IEnumerable<GenerationTask> GetAllTasks()
        {
            return _tasks.Values.OrderByDescending(t => t.CreatedAt);
        }
        
        /// <summary>
        /// Gets all generation tasks asynchronously
        /// </summary>
        public async Task<IEnumerable<GenerationTask>> GetAllTasksAsync()
        {
            return await Task.FromResult(_tasks.Values.OrderByDescending(t => t.CreatedAt));
        }
        
        /// <summary>
        /// Gets a specific task by ID
        /// </summary>
        public GenerationTask? GetTask(string taskId)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }
        
        /// <summary>
        /// Cancels a pending or queued task
        /// </summary>
        public async Task<bool> CancelTaskAsync(string taskId)
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
                            var cancelled = await CancelTaskInComfyUI(task);
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
        /// Submits a task using the appropriate generation service
        /// </summary>
        private async Task SubmitTaskAsync<TConfig>(GenerationTask task, IGenerationService<TConfig> service, TConfig config) where TConfig : class
        {
            _logger.LogInformation("Starting submission of task {TaskId} ({TaskName}) - Type: {TaskType}", 
                task.Id, task.Name, task.Type);
            
            try
            {
                task.Status = GenerationStatus.Queued;
                task.SubmittedAt = DateTime.UtcNow;
                
                var promptId = await service.SubmitTaskAsync(task, config);
                
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
                
                TaskStatusChanged?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
                task.ErrorMessage = $"{task.Type} generation failed: {ex.Message}";
                
                _logger.LogError(ex, "✗ Error submitting {TaskType} task {TaskId} ({TaskName}) to ComfyUI. Error: {ErrorMessage}", 
                    task.Type, task.Id, task.Name, ex.Message);
                TaskStatusChanged?.Invoke(task);
            }
        }

        /// <summary>
        /// Cancel a task in ComfyUI using the appropriate service
        /// </summary>
        private async Task<bool> CancelTaskInComfyUI(GenerationTask task)
        {
            return task.Type switch
            {
                GenerationType.Audio => await (_audioGenerationService.CancelTaskAsync(task.PromptId!) ?? Task.FromResult(false)),
                GenerationType.Image => await (_imageGenerationService.CancelTaskAsync(task.PromptId!) ?? Task.FromResult(false)),
                GenerationType.Video => await (_videoGenerationService.CancelTaskAsync(task.PromptId!) ?? Task.FromResult(false)),
                _ => false
            };
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
                
                // Early exit if no active tasks
                if (activeTasks.Count == 0)
                {
                    return;
                }
                
                _logger.LogDebug("Checking {ActiveCount} active tasks for completion (Total tasks in queue: {TotalCount})", activeTasks.Count, allTasks.Count);
                
                if (allTasks.Any())
                {
                    var statusSummary = allTasks.GroupBy(t => t.Status)
                        .Select(g => $"{g.Key}: {g.Count()}")
                        .ToList();
                    _logger.LogDebug("Task queue summary: {StatusSummary}", string.Join(", ", statusSummary));
                }
                
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
        /// Checks if a specific task has completed and downloads the file
        /// </summary>
        private async Task CheckTaskCompletionAsync(GenerationTask task)
        {
            try
            {
                string? filePath = null;
                
                switch (task.Type)
                {
                    case GenerationType.Audio:
                        {
                                filePath = await _audioGenerationService.CheckTaskCompletionAsync(task);
                        }
                        break;
                    case GenerationType.Image:
                        {
                            filePath = await _imageGenerationService.CheckTaskCompletionAsync(task);
                        }
                        break;
                    case GenerationType.Video:
                        {
                            filePath = await _videoGenerationService.CheckTaskCompletionAsync(task);
                        }
                        break;
                    default:
                        _logger.LogWarning("No generation service available for task {TaskId} of type {TaskType}", task.Id, task.Type);
                        return;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    _logger.LogInformation("Invoking TaskStatusChanged event for completed task {TaskId}", task.Id);
                    TaskStatusChanged?.Invoke(task);
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
        /// Gets available audio generation models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetAudioModelsAsync()
        {
            return await _audioGenerationService.GetModelsAsync(); 
        }
        
        /// <summary>
        /// Gets available image generation models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetImageModelsAsync()
        {

            return await _imageGenerationService.GetModelsAsync();
        }
        
        /// <summary>
        /// Gets available video generation models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetVideoModelsAsync()
        {
            return await _videoGenerationService.GetModelsAsync();
        }

        /// <summary>
        /// Gets available CLIP (text encoder) models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetCLIPModelsAsync()
        {
            return await _audioGenerationService.GetCLIPModelsAsync();
        }

        /// <summary>
        /// Gets available VAE models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetVAEModelsAsync()
        {

            return await _audioGenerationService.GetVAEModelsAsync();
        }

        /// <summary>
        /// Gets available UNET (diffusion model) models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetUNETModelsAsync()
        {
            return await _videoGenerationService.GetUNETModelsAsync();
        }

        /// <summary>
        /// Gets available Audio Encoder models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetAudioEncoderModelsAsync()
        {

            return await _audioGenerationService.GetAudioEncoderModelsAsync();
        }

        /// <summary>
        /// Gets available LoRA models from ComfyUI
        /// </summary>
        public async Task<List<string>> GetLoRAModelsAsync()
        {
            return await _videoGenerationService.GetLoRAModelsAsync();
        }
        
        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}