using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Image generation workflow service - depends on GenerationQueueService
    /// </summary>
    public class ImageGenerationWorkflow
    {
        private readonly IGenerationQueueService _queueService;
        private readonly IComfyUIImageService _imageService;
        private readonly ILogger<ImageGenerationWorkflow> _logger;

        public ImageGenerationWorkflow(
            IGenerationQueueService queueService,
            IComfyUIImageService imageService,
            ILogger<ImageGenerationWorkflow> logger)
        {
            _queueService = queueService;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Generate image with the specified configuration
        /// </summary>
        public async Task<string> GenerateAsync(string name, ImageWorkflowConfig config, string? notes = null)
        {
            _logger.LogInformation("Starting image generation: {Name}", name);

            // Create image generation task
            var task = new ImageGenerationTask(config, _imageService)
            {
                Name = name,
                Notes = notes
            };

            // Queue the task
            var taskId = await _queueService.QueueTaskAsync(task);

            _logger.LogInformation("Image generation queued with task ID: {TaskId}", taskId);
            return taskId;
        }

        /// <summary>
        /// Get available image models
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                return await _imageService.GetImageModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image models");
                return new List<string>();
            }
        }
    }
}