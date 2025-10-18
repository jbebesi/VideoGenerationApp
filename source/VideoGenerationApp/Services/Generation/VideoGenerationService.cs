using VideoGenerationApp.Dto;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling video generation tasks using VideoWorkflowConfig
    /// </summary>
    public class VideoGenerationService : GenerationServiceBase<VideoWorkflowConfig>
    {
        private readonly IComfyUIVideoService _videoService;
        private readonly IComfyUIApiClient _comfyUIClient;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VideoGenerationService(
            IComfyUIVideoService videoService,
            IComfyUIApiClient comfyUIClient,
            IWebHostEnvironment webHostEnvironment,
            ILogger<VideoGenerationService> logger)
            : base(null!, logger)
        {
            _videoService = videoService;
            _comfyUIClient = comfyUIClient;
            _webHostEnvironment = webHostEnvironment;
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
                Type = GenerationType.Video,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, VideoWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Submitting VIDEO generation task {TaskId}", task.Id);

                // For now, return placeholder until full workflow implementation is complete
                // TODO: Implement proper video workflow generation using VideoWorkflowConfig
                
                task.Status = GenerationStatus.Failed;
                task.ErrorMessage = "Video workflow implementation in progress";
                
                return null;
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error submitting video task {TaskId}", task.Id);
                throw;
            }
        }

        public override async Task<List<string>> GetModelsAsync()
        {
            try
            {
                return await _videoService.GetVideoModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving video models");
                return new List<string>();
            }
        }

        public override async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            return await _videoService.GetQueueStatusAsync();
        }

        public override async Task<bool> CancelTaskAsync(string promptId)
        {
            return await _videoService.CancelJobAsync(promptId);
        }

        public override async Task<List<string>> GetUNETModelsAsync()
        {
            return await _videoService.GetUNETModelsAsync();
        }

        public override async Task<List<string>> GetLoRAModelsAsync()
        {
            return await _videoService.GetLoRAModelsAsync();
        }

        protected override async Task<string?> GetGeneratedFileAsync(string promptId)
        {
            return await _videoService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
        }
    }
}