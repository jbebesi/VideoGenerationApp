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
        
        /// <summary>
        /// Full resolved path to the image file (if provided)
        /// </summary>
        public string? ImageFullPath { get; private set; }
        
        /// <summary>
        /// Full resolved path to the audio file (if provided)
        /// </summary>
        public string? AudioFullPath { get; private set; }
        
        /// <summary>
        /// Uploaded filename for the image in ComfyUI (if uploaded)
        /// </summary>
        public string? UploadedImageFilename { get; private set; }
        
        /// <summary>
        /// Uploaded filename for the audio in ComfyUI (if uploaded)
        /// </summary>
        public string? UploadedAudioFilename { get; private set; }
        
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
                if (!string.IsNullOrEmpty(Config.ImageFilePath))
                {
                    UploadedImageFilename = await UploadImageToComfyUIAsync(Config.ImageFilePath);
                    if (UploadedImageFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload image file {Config.ImageFilePath} to ComfyUI. Full path: {ImageFullPath}");
                    }
                }

                // Upload audio file to ComfyUI if provided
                if (!string.IsNullOrEmpty(Config.AudioFilePath))
                {
                    UploadedAudioFilename = await UploadAudioToComfyUIAsync(Config.AudioFilePath);
                    if (UploadedAudioFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload audio file {Config.AudioFilePath} to ComfyUI. Full path: {AudioFullPath}");
                    }
                }

                // Update the config with uploaded filenames for the workflow
                var workflowConfig = Config;
                if (UploadedImageFilename != null || UploadedAudioFilename != null)
                {
                    workflowConfig = new VideoWorkflowConfig
                    {
                        ImageFilePath = UploadedImageFilename ?? Config.ImageFilePath,
                        AudioFilePath = UploadedAudioFilename ?? Config.AudioFilePath,
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
                throw;
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
                // Resolve the full path - handle both absolute and relative paths cross-platform
                string fullImagePath;
                if (Path.IsPathRooted(imageFilePath))
                {
                    fullImagePath = imageFilePath;
                }
                else
                {
                    // Normalize path separators for cross-platform compatibility
                    var normalizedPath = imageFilePath.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar)
                                                     .TrimStart(Path.DirectorySeparatorChar);
                    
                    // If it's a relative path, assume it's relative to wwwroot
                    fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, normalizedPath);
                }

                // Normalize the final path and store it
                fullImagePath = Path.GetFullPath(fullImagePath);
                ImageFullPath = fullImagePath;

                if (!File.Exists(fullImagePath))
                {
                    return null;
                }

                var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                var filename = Path.GetFileName(fullImagePath);
                
                // Validate file is an image by checking extension
                var extension = Path.GetExtension(filename).ToLowerInvariant();
                var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
                if (!validImageExtensions.Contains(extension))
                {
                    return null;
                }
                
                var uploadResponse = await _comfyUIClient.UploadFileAsync(
                    imageBytes, 
                    filename, 
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
                // Resolve the full path - handle both absolute and relative paths cross-platform
                string fullAudioPath;
                if (Path.IsPathRooted(audioFilePath))
                {
                    fullAudioPath = audioFilePath;
                }
                else
                {
                    // Normalize path separators for cross-platform compatibility
                    var normalizedPath = audioFilePath.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar)
                                                     .TrimStart(Path.DirectorySeparatorChar);
                    
                    // If it's a relative path, assume it's relative to wwwroot
                    fullAudioPath = Path.Combine(_webHostEnvironment.WebRootPath, normalizedPath);
                }

                // Normalize the final path and store it
                fullAudioPath = Path.GetFullPath(fullAudioPath);
                AudioFullPath = fullAudioPath;

                if (!File.Exists(fullAudioPath))
                {
                    return null;
                }

                var audioBytes = await File.ReadAllBytesAsync(fullAudioPath);
                var filename = Path.GetFileName(fullAudioPath);
                
                // Validate file is an audio file by checking extension
                var extension = Path.GetExtension(filename).ToLowerInvariant();
                var validAudioExtensions = new[] { ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".aac", ".wma" };
                if (!validAudioExtensions.Contains(extension))
                {
                    return null;
                }
                
                // Use the image upload endpoint for audio files since ComfyUI doesn't have a dedicated audio upload
                // The file will be stored in the input directory and can be referenced by audio nodes
                var uploadResponse = await _comfyUIClient.UploadFileAsync(
                    audioBytes, 
                    filename, 
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
        
        /// <summary>
        /// Gets a summary of file information for debugging/logging
        /// </summary>
        public override string GetFileInfoSummary()
        {
            var summary = new List<string>();
            
            if (!string.IsNullOrEmpty(Config.ImageFilePath))
            {
                summary.Add($"Image: {Config.ImageFilePath} -> {ImageFullPath} -> {UploadedImageFilename ?? "not uploaded"}");
            }
            
            if (!string.IsNullOrEmpty(Config.AudioFilePath))
            {
                summary.Add($"Audio: {Config.AudioFilePath} -> {AudioFullPath} -> {UploadedAudioFilename ?? "not uploaded"}");
            }
            
            return summary.Any() ? string.Join("; ", summary) : "No files";
        }
    }
}