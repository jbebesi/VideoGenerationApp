using VideoGenerationApp.Services;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Image generation task
    /// </summary>
    public class ImageGenerationTask : GenerationTaskBase
    {
        private readonly IComfyUIImageService _imageService;
        
        /// <summary>
        /// Image configuration used for this generation
        /// </summary>
        public ImageWorkflowConfig Config { get; set; }
        
        public override GenerationType Type => GenerationType.Image;
        protected override string OutputSubfolder => "image";
        protected override string FilePrefix => "image";
        
        public ImageGenerationTask(ImageWorkflowConfig config, IComfyUIImageService imageService)
        {
            Config = config;
            _imageService = imageService;
            PositivePrompt = config.PositivePrompt;
        }
        
        public override async Task<string?> SubmitAsync()
        {
            try
            {
                var workflow = ImageWorkflowFactory.CreateWorkflow(Config);
                var workflowDict = _imageService.ConvertWorkflowToComfyUIFormat(workflow);
                return await _imageService.SubmitWorkflowAsync(workflowDict);
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
                    
                var queueStatus = await _imageService.GetQueueStatusAsync();
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
                var filePath = await _imageService.GetGeneratedFileAsync(PromptId, OutputSubfolder, FilePrefix);
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
                    
                return await _imageService.CancelJobAsync(PromptId);
            }
            catch
            {
                return false;
            }
        }
    }
}