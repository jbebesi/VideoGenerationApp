using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling video generation tasks
    /// </summary>
    public class VideoGenerationService : GenerationServiceBase<VideoWorkflowConfig>
    {
        public VideoGenerationService(IServiceScopeFactory serviceScopeFactory, ILogger<VideoGenerationService> logger)
            : base(serviceScopeFactory, logger)
        {
        }

        public override GenerationType Type => GenerationType.Video;
        protected override string OutputSubfolder => "video";
        protected override string FilePrefix => "video";

        public override GenerationTask CreateTask(string name, VideoWorkflowConfig config, string? notes = null)
        {
            return new GenerationTask
            {
                Name = name,
                PositivePrompt = config.TextPrompt,
                VideoConfig = config,
                Type = GenerationType.Video,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, VideoWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Submitting VIDEO generation task {TaskId} to ComfyUI", task.Id);
                
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                
                var workflow = VideoWorkflowFactory.CreateWorkflow(config);
                var workflowDict = videoService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Video workflow prepared for task {TaskId} with {NodeCount} nodes - Duration: {Duration}s, Size: {Width}x{Height}@{Fps}fps", 
                    task.Id, workflowDict.Count, config.DurationSeconds, 
                    config.Width, config.Height, config.Fps);
                
                // Log the workflow being sent (detailed debug info)
                var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflowDict, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Video workflow JSON for task {TaskId}: {WorkflowJson}", task.Id, workflowJson);
                
                var promptId = await videoService.SubmitWorkflowAsync(workflowDict);
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    _logger.LogInformation("Video workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                        task.Id, promptId);
                }
                
                return promptId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting video task {TaskId} to ComfyUI", task.Id);
                throw;
            }
        }

        public override async Task<List<string>> GetModelsAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.GetVideoModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting video models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.GetQueueStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status from ComfyUI video service");
                return null;
            }
        }

        public override async Task<bool> CancelTaskAsync(string promptId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.CancelJobAsync(promptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling video task with prompt ID {PromptId}", promptId);
                return false;
            }
        }

        public override async Task<List<string>> GetUNETModelsAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.GetUNETModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UNET models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<List<string>> GetLoRAModelsAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.GetLoRAModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LoRA models from ComfyUI");
                return new List<string>();
            }
        }

        protected override async Task<string?> GetGeneratedFileAsync(string promptId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var videoService = scope.ServiceProvider.GetRequiredService<IComfyUIVideoService>();
                return await videoService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated video file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}