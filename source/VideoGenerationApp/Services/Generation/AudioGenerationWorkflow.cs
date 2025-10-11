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

            // Create audio generation task
            var task = new AudioGenerationTask(config, _audioService)
            {
                Name = name,
                Notes = notes
            };

            // Queue the task
            var taskId = await _queueService.QueueTaskAsync(task);

            _logger.LogInformation("Audio generation queued with task ID: {TaskId}", taskId);
            return taskId;
        }

        /// <summary>
        /// Get available audio models
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                return await _audioService.GetAudioModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audio models");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available CLIP models
        /// </summary>
        public async Task<List<string>> GetCLIPModelsAsync()
        {
            try
            {
                return await _audioService.GetCLIPModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CLIP models");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available VAE models
        /// </summary>
        public async Task<List<string>> GetVAEModelsAsync()
        {
            try
            {
                return await _audioService.GetVAEModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VAE models");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available Audio Encoder models
        /// </summary>
        public async Task<List<string>> GetAudioEncoderModelsAsync()
        {
            try
            {
                return await _audioService.GetAudioEncoderModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Audio Encoder models");
                return new List<string>();
            }
        }
    }
}