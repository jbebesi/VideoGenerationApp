using VideoGenerationApp.Services;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Audio generation task
    /// </summary>
    public class AudioGenerationTask : GenerationTaskBase
    {
        private readonly IComfyUIAudioService _audioService;
        
        /// <summary>
        /// Audio configuration used for this generation
        /// </summary>
        public AudioWorkflowConfig Config { get; set; }
        
        public override GenerationType Type => GenerationType.Audio;
        protected override string OutputSubfolder => "audio";
        protected override string FilePrefix => "audio";
        
        public AudioGenerationTask(AudioWorkflowConfig config, IComfyUIAudioService audioService)
        {
            Config = config;
            _audioService = audioService;
            PositivePrompt = $"{config.Tags} - {config.Lyrics?.Substring(0, Math.Min(50, config.Lyrics?.Length ?? 0))}";
        }
        
        public override async Task<string?> SubmitAsync()
        {
            try
            {
                var workflow = AudioWorkflowFactory.CreateWorkflow(Config);
                var workflowDict = _audioService.ConvertWorkflowToComfyUIFormat(workflow);
                return await _audioService.SubmitWorkflowAsync(workflowDict);
            }
            catch
            {
                return null;
            }
        }
        
        public override async Task<string?> CheckCompletionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(PromptId))
                    return null;
                    
                var queueStatus = await _audioService.GetQueueStatusAsync();
                var isInQueue = queueStatus?.queue?.Any(q => q.prompt_id == PromptId) == true;
                var isExecuting = queueStatus?.exec?.Any(q => q.prompt_id == PromptId) == true;
                
                if (isInQueue || isExecuting)
                {
                    // Update status and queue position
                    var previousStatus = Status;
                    
                    if (isExecuting && Status != GenerationStatus.Processing)
                    {
                        Status = GenerationStatus.Processing;
                        QueuePosition = 0;
                    }
                    else if (isInQueue && Status != GenerationStatus.Queued)
                    {
                        Status = GenerationStatus.Queued;
                        var queueItem = queueStatus?.queue?.FirstOrDefault(q => q.prompt_id == PromptId);
                        if (queueItem != null && queueStatus?.queue != null)
                        {
                            QueuePosition = queueStatus.queue.ToList().IndexOf(queueItem) + 1;
                        }
                    }
                    
                    return null; // Still in progress
                }
                
                // Task completed, try to download the file
                var filePath = await _audioService.GetGeneratedFileAsync(PromptId, OutputSubfolder, FilePrefix);
                if (!string.IsNullOrEmpty(filePath))
                {
                    Status = GenerationStatus.Completed;
                    GeneratedFilePath = filePath;
                    CompletedAt = DateTime.UtcNow;
                    QueuePosition = null;
                    return filePath;
                }
                
                return null;
            }
            catch
            {
                Status = GenerationStatus.Failed;
                CompletedAt = DateTime.UtcNow;
                QueuePosition = null;
                return null;
            }
        }
        
        public override async Task<bool> CancelAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(PromptId))
                    return false;
                    
                return await _audioService.CancelJobAsync(PromptId);
            }
            catch
            {
                return false;
            }
        }
    }
}