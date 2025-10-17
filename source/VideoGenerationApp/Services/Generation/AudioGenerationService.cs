using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling audio generation tasks
    /// </summary>
    public class AudioGenerationService : GenerationServiceBase<AudioWorkflowConfig>
    {
        private readonly IComfyUIAudioService _audioService;

        public AudioGenerationService(
            IComfyUIAudioService audioService,
            ILogger<AudioGenerationService> logger)
            : base(null!, logger) // Pass null for service scope factory since we're injecting services directly
        {
            _audioService = audioService;
        }

        public override GenerationType Type => GenerationType.Audio;
        protected override string OutputSubfolder => "audio";
        protected override string FilePrefix => "audio";

        public override GenerationTask CreateTask(string name, AudioWorkflowConfig config, string? notes = null)
        {
            return new GenerationTask
            {
                Name = name,
                PositivePrompt = $"{config.Tags} - {config.Lyrics?.Substring(0, Math.Min(50, config.Lyrics?.Length ?? 0))}",
                AudioConfig = config,
                Type = GenerationType.Audio,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, AudioWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Submitting AUDIO generation task {TaskId} to ComfyUI", task.Id);
                
                var workflow = AudioWorkflowFactory.CreateWorkflow(config);
                var workflowDict = _audioService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Audio workflow prepared for task {TaskId} with {NodeCount} nodes", task.Id, workflowDict.Count);
                
                var promptId = await _audioService.SubmitWorkflowAsync(workflowDict);
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    _logger.LogInformation("Audio workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                        task.Id, promptId);
                }
                
                return promptId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting audio task {TaskId} to ComfyUI", task.Id);
                throw;
            }
        }

        public override async Task<List<string>> GetModelsAsync()
        {
            try
            {
                return await _audioService.GetAudioModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audio models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            try
            {
                return await _audioService.GetQueueStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status from ComfyUI audio service");
                return null;
            }
        }

        public override async Task<bool> CancelTaskAsync(string promptId)
        {
            try
            {
                return await _audioService.CancelJobAsync(promptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling audio task with prompt ID {PromptId}", promptId);
                return false;
            }
        }

        public override async Task<List<string>> GetCLIPModelsAsync()
        {
            try
            {
                return await _audioService.GetCLIPModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CLIP models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<List<string>> GetVAEModelsAsync()
        {
            try
            {
                return await _audioService.GetVAEModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VAE models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<List<string>> GetAudioEncoderModelsAsync()
        {
            try
            {
                return await _audioService.GetAudioEncoderModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Audio Encoder models from ComfyUI");
                return new List<string>();
            }
        }

        protected override async Task<string?> GetGeneratedFileAsync(string promptId)
        {
            try
            {
                return await _audioService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated audio file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}