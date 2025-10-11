using VideoGenerationApp.Services;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Video generation task
    /// </summary>
    public class VideoGenerationTask : GenerationTaskBase
    {
        private readonly IComfyUIVideoService _videoService;
        private readonly IComfyUIApiClient _comfyUIClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        /// <summary>
        /// Video configuration used for this generation
        /// </summary>
        public VideoWorkflowConfig Config { get; set; }
        
        public override GenerationType Type => GenerationType.Video;
        protected override string OutputSubfolder => "video";
        protected override string FilePrefix => "video";
        
        public VideoGenerationTask(
            VideoWorkflowConfig config, 
            IComfyUIVideoService videoService,
            IComfyUIApiClient comfyUIClient,
            IWebHostEnvironment webHostEnvironment)
        {
            Config = config;
            _videoService = videoService;
            _comfyUIClient = comfyUIClient;
            _webHostEnvironment = webHostEnvironment;
            PositivePrompt = config.TextPrompt;
        }
        
        public override async Task<string?> SubmitAsync()
        {
            try
            {
                // Upload image file to ComfyUI if provided
                string? uploadedImageFilename = null;
                if (!string.IsNullOrEmpty(Config.ImageFilePath))
                {
                    uploadedImageFilename = await UploadImageToComfyUIAsync(Config.ImageFilePath);
                    if (uploadedImageFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload image file {Config.ImageFilePath} to ComfyUI");
                    }
                }

                // Upload audio file to ComfyUI if provided
                string? uploadedAudioFilename = null;
                if (!string.IsNullOrEmpty(Config.AudioFilePath))
                {
                    uploadedAudioFilename = await UploadAudioToComfyUIAsync(Config.AudioFilePath);
                    if (uploadedAudioFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload audio file {Config.AudioFilePath} to ComfyUI");
                    }
                }

                // Update the config with uploaded filenames for the workflow
                var workflowConfig = Config;
                if (uploadedImageFilename != null || uploadedAudioFilename != null)
                {
                    workflowConfig = new VideoWorkflowConfig
                    {
                        ImageFilePath = uploadedImageFilename ?? Config.ImageFilePath,
                        AudioFilePath = uploadedAudioFilename ?? Config.AudioFilePath,
                        TextPrompt = Config.TextPrompt,
                        DurationSeconds = Config.DurationSeconds,
                        Width = Config.Width,
                        Height = Config.Height,
                        Fps = Config.Fps,
                        AnimationStyle = Config.AnimationStyle,
                        MotionIntensity = Config.MotionIntensity,
                        CheckpointName = Config.CheckpointName,
                        Seed = Config.Seed,
                        Steps = Config.Steps,
                        CFGScale = Config.CFGScale,
                        AugmentationLevel = Config.AugmentationLevel,
                        OutputFilename = Config.OutputFilename,
                        OutputFormat = Config.OutputFormat,
                        Quality = Config.Quality
                    };
                }

                var workflow = VideoWorkflowFactory.CreateWorkflow(workflowConfig);
                var workflowDict = _videoService.ConvertWorkflowToComfyUIFormat(workflow);
                return await _videoService.SubmitWorkflowAsync(workflowDict);
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
                    
                var queueStatus = await _videoService.GetQueueStatusAsync();
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
                var filePath = await _videoService.GetGeneratedFileAsync(PromptId, OutputSubfolder, FilePrefix);
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
                    
                return await _videoService.CancelJobAsync(PromptId);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Uploads an image file to ComfyUI and returns the uploaded filename
        /// </summary>
        private async Task<string?> UploadImageToComfyUIAsync(string imageFilePath)
        {
            try
            {
                // Resolve the full path - handle both absolute and relative paths
                string fullImagePath;
                if (Path.IsPathRooted(imageFilePath))
                {
                    fullImagePath = imageFilePath;
                }
                else
                {
                    // If it's a relative path, assume it's relative to wwwroot
                    fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageFilePath.TrimStart('/'));
                }

                if (!File.Exists(fullImagePath))
                {
                    return null;
                }

                var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                var filename = Path.GetFileName(fullImagePath);
                
                var uploadResponse = await _comfyUIClient.UploadImageAsync(
                    imageBytes, 
                    filename, 
                    subfolder: "input",
                    overwrite: true
                );

                return uploadResponse.Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Uploads an audio file to ComfyUI and returns the uploaded filename
        /// </summary>
        private async Task<string?> UploadAudioToComfyUIAsync(string audioFilePath)
        {
            try
            {
                // Resolve the full path - handle both absolute and relative paths
                string fullAudioPath;
                if (Path.IsPathRooted(audioFilePath))
                {
                    fullAudioPath = audioFilePath;
                }
                else
                {
                    // If it's a relative path, assume it's relative to wwwroot
                    fullAudioPath = Path.Combine(_webHostEnvironment.WebRootPath, audioFilePath.TrimStart('/'));
                }

                if (!File.Exists(fullAudioPath))
                {
                    return null;
                }

                var audioBytes = await File.ReadAllBytesAsync(fullAudioPath);
                var filename = Path.GetFileName(fullAudioPath);
                
                // Use the image upload endpoint for audio files since ComfyUI doesn't have a dedicated audio upload
                var uploadResponse = await _comfyUIClient.UploadImageAsync(
                    audioBytes, 
                    filename, 
                    subfolder: "input",
                    type: "input",
                    overwrite: true
                );

                return uploadResponse.Name;
            }
            catch
            {
                return null;
            }
        }
    }
}