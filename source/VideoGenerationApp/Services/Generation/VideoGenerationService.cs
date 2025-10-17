using VideoGenerationApp.Dto;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling video generation tasks using new ComfyUIWorkflow builder
    /// </summary>
    public class VideoGenerationService : GenerationServiceBase<ComfyUIWorkflow>
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

        public override GenerationTask CreateTask(string name, ComfyUIWorkflow config, string? notes = null)
        {
            return new GenerationTask
            {
                Name = name,
                PositivePrompt = "", // will be filled later
                Type = GenerationType.Video,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, ComfyUIWorkflow config)
        {
            try
            {
                _logger.LogInformation("Submitting VIDEO generation task {TaskId}", task.Id);

                // Build prompt JSON from provided workflow config
                var promptDict = config.ToPromptDictionary();
                var promptId = await _videoService.SubmitWorkflowAsync(promptDict);

                if (!string.IsNullOrEmpty(promptId))
                {
                    task.PromptId = promptId;
                    task.SubmittedAt = DateTime.UtcNow;
                    task.Status = GenerationStatus.Queued;
                    _logger.LogInformation("Task {TaskId} submitted with prompt ID {PromptId}", task.Id, promptId);
                }
                else
                {
                    task.Status = GenerationStatus.Failed;
                }

                return promptId;
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