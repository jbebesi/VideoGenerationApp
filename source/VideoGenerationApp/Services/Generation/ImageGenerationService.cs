using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling image generation tasks
    /// </summary>
    public class ImageGenerationService : GenerationServiceBase<ImageWorkflowConfig>
    {
        private readonly IComfyUIImageService _imageService;

        public ImageGenerationService(
            IComfyUIImageService imageService,
            ILogger<ImageGenerationService> logger)
            : base(null!, logger) // Pass null for service scope factory since we're injecting services directly
        {
            _imageService = imageService;
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
                
                var workflow = ImageWorkflowFactory.CreateWorkflow(config);
                var workflowDict = _imageService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Image workflow prepared for task {TaskId} with {NodeCount} nodes - Size: {Width}x{Height}", 
                    task.Id, workflowDict.Count, config.Width, config.Height);
                
                // Log the workflow being sent (detailed debug info)
                var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflowDict, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Image workflow JSON for task {TaskId}: {WorkflowJson}", task.Id, workflowJson);
                
                var promptId = await _imageService.SubmitWorkflowAsync(workflowDict);
                
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
                return await _imageService.GetImageModelsAsync();
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
                return await _imageService.GetQueueStatusAsync();
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
                return await _imageService.CancelJobAsync(promptId);
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
                return await _imageService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated image file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}