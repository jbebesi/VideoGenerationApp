using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Audio generation workflow service - depends on GenerationQueueService
    /// </summary>
    public class AudioGenerationWorkflow
    {
        private readonly IGenerationQueueService _queueService;
        private readonly IComfyUIAudioService _audioService;
        private readonly ILogger<AudioGenerationWorkflow> _logger;

        public AudioGenerationWorkflow(
            IGenerationQueueService queueService,
            IComfyUIAudioService audioService,
            ILogger<AudioGenerationWorkflow> logger)
        {
            _queueService = queueService;
            _audioService = audioService;
            _logger = logger;
        }

        /// <summary>
        /// Generate audio with the specified configuration
        /// </summary>
        public async Task<string> GenerateAsync(string name, AudioWorkflowConfig config, string? notes = null)
        {
            _logger.LogInformation("Starting audio generation: {Name}", name);

            var task = new AudioGenerationTask(config, _audioService)
            {
                Name = name,
                Notes = notes
            };

            var taskId = await _queueService.QueueTaskAsync(task);

            _logger.LogInformation("Audio generation queued with task ID: {TaskId}", taskId);
            return taskId;
        }
    }
}