using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling image generation tasks
    /// </summary>
    public class ImageGenerationService : GenerationServiceBase<ImageWorkflowConfig>
    {
        public ImageGenerationService(IServiceScopeFactory serviceScopeFactory, ILogger<ImageGenerationService> logger)
            : base(serviceScopeFactory, logger)
        {
        }

        public override GenerationType Type => GenerationType.Image;
        protected override string OutputSubfolder => "image";
        protected override string FilePrefix => "image";

        public override GenerationTask CreateTask(string name, ImageWorkflowConfig config, string? notes = null)
        {
            return new GenerationTask
            {
                Name = name,
                PositivePrompt = config.PositivePrompt,
                ImageConfig = config,
                Type = GenerationType.Image,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, ImageWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Submitting IMAGE generation task {TaskId} to ComfyUI", task.Id);
                
                using var scope = _serviceScopeFactory.CreateScope();
                var imageService = scope.ServiceProvider.GetRequiredService<IComfyUIImageService>();
                
                var workflow = ImageWorkflowFactory.CreateWorkflow(config);
                var workflowDict = imageService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Image workflow prepared for task {TaskId} with {NodeCount} nodes - Size: {Width}x{Height}", 
                    task.Id, workflowDict.Count, config.Width, config.Height);
                
                // Log the workflow being sent (detailed debug info)
                var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflowDict, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Image workflow JSON for task {TaskId}: {WorkflowJson}", task.Id, workflowJson);
                
                var promptId = await imageService.SubmitWorkflowAsync(workflowDict);
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    _logger.LogInformation("Image workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                        task.Id, promptId);
                }
                
                return promptId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting image task {TaskId} to ComfyUI", task.Id);
                throw;
            }
        }

        public override async Task<List<string>> GetModelsAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var imageService = scope.ServiceProvider.GetRequiredService<IComfyUIImageService>();
                return await imageService.GetImageModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var imageService = scope.ServiceProvider.GetRequiredService<IComfyUIImageService>();
                return await imageService.GetQueueStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status from ComfyUI image service");
                return null;
            }
        }

        public override async Task<bool> CancelTaskAsync(string promptId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var imageService = scope.ServiceProvider.GetRequiredService<IComfyUIImageService>();
                return await imageService.CancelJobAsync(promptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling image task with prompt ID {PromptId}", promptId);
                return false;
            }
        }

        protected override async Task<string?> GetGeneratedFileAsync(string promptId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var imageService = scope.ServiceProvider.GetRequiredService<IComfyUIImageService>();
                return await imageService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated image file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}