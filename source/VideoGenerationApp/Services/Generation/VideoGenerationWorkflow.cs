using VideoGenerationApp.Dto;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Video generation workflow service - depends on GenerationQueueService
    /// </summary>
    public class VideoGenerationWorkflow
    {
        private readonly IGenerationQueueService _queueService;
        private readonly IComfyUIVideoService _videoService;
        private readonly IComfyUIApiClient _comfyUIClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<VideoGenerationWorkflow> _logger;

        public VideoGenerationWorkflow(
            IGenerationQueueService queueService,
            IComfyUIVideoService videoService,
            IComfyUIApiClient comfyUIClient,
            IWebHostEnvironment webHostEnvironment,
            ILogger<VideoGenerationWorkflow> logger)
        {
            _queueService = queueService;
            _videoService = videoService;
            _comfyUIClient = comfyUIClient;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// Generate video with the specified configuration
        /// </summary>
        public async Task<string> GenerateAsync(string name, VideoWorkflowConfig config, string? notes = null)
        {
            _logger.LogInformation("Starting video generation: {Name}", name);

            // Create video generation task
            var task = new VideoGenerationTask(config, _videoService, _comfyUIClient, _webHostEnvironment)
            {
                Name = name,
                Notes = notes
            };

            // Queue the task
            var taskId = await _queueService.QueueTaskAsync(task);

            _logger.LogInformation("Video generation queued with task ID: {TaskId}", taskId);
            return taskId;
        }

        /// <summary>
        /// Get available video models
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                return await _videoService.GetVideoModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting video models");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available UNET models
        /// </summary>
        public async Task<List<string>> GetUNETModelsAsync()
        {
            try
            {
                return await _videoService.GetUNETModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UNET models");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available LoRA models
        /// </summary>
        public async Task<List<string>> GetLoRAModelsAsync()
        {
            try
            {
                return await _videoService.GetLoRAModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LoRA models");
                return new List<string>();
            }
        }
    }
}