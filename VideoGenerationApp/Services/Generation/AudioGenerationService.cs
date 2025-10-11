using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling audio generation tasks
    /// </summary>
    public class AudioGenerationService : GenerationServiceBase<AudioWorkflowConfig>
    {
        public AudioGenerationService(IServiceScopeFactory serviceScopeFactory, ILogger<AudioGenerationService> logger)
            : base(serviceScopeFactory, logger)
        {
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
                
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                
                var workflow = AudioWorkflowFactory.CreateWorkflow(config);
                var workflowDict = audioService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Audio workflow prepared for task {TaskId} with {NodeCount} nodes", task.Id, workflowDict.Count);
                
                var promptId = await audioService.SubmitWorkflowAsync(workflowDict);
                
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetAudioModelsAsync();
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetQueueStatusAsync();
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.CancelJobAsync(promptId);
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetCLIPModelsAsync();
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetVAEModelsAsync();
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetAudioEncoderModelsAsync();
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
                using var scope = _serviceScopeFactory.CreateScope();
                var audioService = scope.ServiceProvider.GetRequiredService<IComfyUIAudioService>();
                return await audioService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated audio file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}