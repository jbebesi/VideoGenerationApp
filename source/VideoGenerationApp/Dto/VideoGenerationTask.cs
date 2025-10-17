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
        private readonly VideoWorkflowWrapper _originalWrapper;
        
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
            VideoWorkflowWrapper wrapper, 
            IComfyUIVideoService videoService,
            IComfyUIApiClient comfyUIClient,
            IWebHostEnvironment webHostEnvironment)
        {
            _originalWrapper = wrapper;
            _videoService = videoService;
            _comfyUIClient = comfyUIClient;
            _webHostEnvironment = webHostEnvironment;
            
            PositivePrompt = wrapper.TextPrompt;
        }
        
        public override async Task<string?> SubmitAsync()
        {
            try
            {
                // Upload image file to ComfyUI if provided
                if (!string.IsNullOrEmpty(_originalWrapper.ImageFilePath))
                {
                    UploadedImageFilename = await UploadImageToComfyUIAsync(_originalWrapper.ImageFilePath);
                    if (UploadedImageFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload image file {_originalWrapper.ImageFilePath} to ComfyUI. Full path: {ImageFullPath}");
                    }
                }

                // Upload audio file to ComfyUI if provided (not currently wired into workflow nodes)
                if (!string.IsNullOrEmpty(_originalWrapper.AudioFilePath))
                {
                    UploadedAudioFilename = await UploadAudioToComfyUIAsync(_originalWrapper.AudioFilePath);
                    if (UploadedAudioFilename == null)
                    {
                        throw new InvalidOperationException($"Failed to upload audio file {_originalWrapper.AudioFilePath} to ComfyUI. Full path: {AudioFullPath}");
                    }
                }


                var audioRef = !string.IsNullOrEmpty(UploadedAudioFilename) ? UploadedAudioFilename : _originalWrapper.AudioFilePath;
                var imageRef = !string.IsNullOrEmpty(UploadedImageFilename) ? UploadedImageFilename : _originalWrapper.ImageFilePath;
                if (string.IsNullOrWhiteSpace(imageRef))
                {
                    throw new InvalidOperationException("An input image is required for video generation.");
                }

                /// TODO FIRST

                var wf = ComfyUIWorkflow.CreateVideoGenerationWorkflow(
                    positivePrompt: _originalWrapper.TextPrompt,
                    negativePrompt: _originalWrapper.NegativePrompt,
                    imagePath: imageRef,
                    audioPath: audioRef,
                    seed: _originalWrapper.Seed <= -1 ? 12345 : _originalWrapper.Seed,
                    steps: _originalWrapper.Steps,
                    cfg: _originalWrapper.CFGScale,
                    fps: _originalWrapper.Fps,
                    //modelName: string.IsNullOrWhiteSpace(_originalWrapper.CheckpointName) ? "wan2.1_t2v_1.3B_fp16.safetensors" : _originalWrapper.CheckpointName,
                    filenamePrefix: string.IsNullOrWhiteSpace(_originalWrapper.OutputFilename) ? "output" : _originalWrapper.OutputFilename,
                    samplerName: _originalWrapper.SamplerName,
                    scheduler: _originalWrapper.Scheduler,
                    denoise: _originalWrapper.Denoise,
                    codec: "h264",
                    format: string.IsNullOrWhiteSpace(_originalWrapper.OutputFormat) ? "mp4" : _originalWrapper.OutputFormat
                );

                var promptDict = wf.ToPromptDictionary();

                var promptId = await _videoService.SubmitWorkflowAsync(promptDict);

                if (!string.IsNullOrEmpty(promptId))
                {
                    PromptId = promptId;
                    SubmittedAt = DateTime.UtcNow;
                }

                return promptId;
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
        
        private async Task<string?> UploadImageToComfyUIAsync(string imageFilePath)
        {
            try
            {
                string fullImagePath;
                if (Path.IsPathRooted(imageFilePath))
                {
                    fullImagePath = imageFilePath;
                }
                else
                {
                    var normalizedPath = imageFilePath.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar)
                                                     .TrimStart(Path.DirectorySeparatorChar);
                    fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, normalizedPath);
                }

                fullImagePath = Path.GetFullPath(fullImagePath);
                ImageFullPath = fullImagePath;

                if (!File.Exists(fullImagePath))
                {
                    return null;
                }

                var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                var filename = Path.GetFileName(fullImagePath);
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

        private async Task<string?> UploadAudioToComfyUIAsync(string audioFilePath)
        {
            try
            {
                string fullAudioPath;
                if (Path.IsPathRooted(audioFilePath))
                {
                    fullAudioPath = audioFilePath;
                }
                else
                {
                    var normalizedPath = audioFilePath.Replace('\\', Path.DirectorySeparatorChar)
                                                     .Replace('/', Path.DirectorySeparatorChar)
                                                     .TrimStart(Path.DirectorySeparatorChar);
                    fullAudioPath = Path.Combine(_webHostEnvironment.WebRootPath, normalizedPath);
                }

                fullAudioPath = Path.GetFullPath(fullAudioPath);
                AudioFullPath = fullAudioPath;

                if (!File.Exists(fullAudioPath))
                {
                    return null;
                }

                var audioBytes = await File.ReadAllBytesAsync(fullAudioPath);
                var filename = Path.GetFileName(fullAudioPath);
                var extension = Path.GetExtension(filename).ToLowerInvariant();
                var validAudioExtensions = new[] { ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".aac", ".wma" };
                if (!validAudioExtensions.Contains(extension))
                {
                    return null;
                }
                
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
        
        public override string GetFileInfoSummary()
        {
            var summary = new List<string>();
            
            if (!string.IsNullOrEmpty(_originalWrapper.ImageFilePath))
            {
                summary.Add($"Image: {_originalWrapper.ImageFilePath} -> {ImageFullPath} -> {UploadedImageFilename ?? "not uploaded"}");
            }
            
            if (!string.IsNullOrEmpty(_originalWrapper.AudioFilePath))
            {
                summary.Add($"Audio: {_originalWrapper.AudioFilePath} -> {AudioFullPath} -> {UploadedAudioFilename ?? "not uploaded"}");
            }
            
            return summary.Any() ? string.Join("; ", summary) : "No files";
        }
    }
}