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
        public async Task<string> GenerateAsync(string name, VideoWorkflowWrapper wrapper, string? notes = null)
        {
            _logger.LogInformation("Starting video generation: {Name}", name);

            var task = new VideoGenerationTask(wrapper, _videoService, _comfyUIClient, _webHostEnvironment)
            {
                Name = name,
                Notes = notes
            };

            

            // Queue the task
            var taskId = await _queueService.QueueTaskAsync(task);

            _logger.LogInformation("Video generation queued with task ID: {TaskId}", taskId);
            return taskId;
        }
    }
}