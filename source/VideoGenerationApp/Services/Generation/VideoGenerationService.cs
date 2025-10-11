using Microsoft.Extensions.DependencyInjection;
using VideoGenerationApp.Dto;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services.Generation
{
    /// <summary>
    /// Service for handling video generation tasks
    /// </summary>
    public class VideoGenerationService : GenerationServiceBase<VideoWorkflowConfig>
    {
        private readonly IComfyUIVideoService _videoService;
        private readonly IComfyUIApiClient _comfyUIClient;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VideoGenerationService(
            IComfyUIVideoService videoService,
            IComfyUIApiClient comfyUIClient,
            IWebHostEnvironment webHostEnvironment,
            ILogger<VideoGenerationService> logger)
            : base(null!, logger) // Pass null for service scope factory since we're injecting services directly
        {
            _videoService = videoService;
            _comfyUIClient = comfyUIClient;
            _webHostEnvironment = webHostEnvironment;
        }

        public override GenerationType Type => GenerationType.Video;
        protected override string OutputSubfolder => "video";
        protected override string FilePrefix => "video";

        public override GenerationTask CreateTask(string name, VideoWorkflowConfig config, string? notes = null)
        {
            return new GenerationTask
            {
                Name = name,
                PositivePrompt = config.TextPrompt,
                VideoConfig = config,
                Type = GenerationType.Video,
                Notes = notes,
                Status = GenerationStatus.Pending
            };
        }

        public override async Task<string?> SubmitTaskAsync(GenerationTask task, VideoWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Submitting VIDEO generation task {TaskId} to ComfyUI", task.Id);
                
                // Step 1: Upload image file to ComfyUI if provided
                string? uploadedImageFilename = null;
                if (!string.IsNullOrEmpty(config.ImageFilePath))
                {
                    uploadedImageFilename = await UploadImageToComfyUIAsync(config.ImageFilePath);
                    if (uploadedImageFilename == null)
                    {
                        _logger.LogError("Failed to upload image file {ImageFilePath} to ComfyUI for task {TaskId}", config.ImageFilePath, task.Id);
                        throw new InvalidOperationException($"Failed to upload image file {config.ImageFilePath} to ComfyUI");
                    }
                    _logger.LogInformation("Successfully uploaded image {ImageFilePath} to ComfyUI as {UploadedFilename} for task {TaskId}", 
                        config.ImageFilePath, uploadedImageFilename, task.Id);
                }

                // Step 2: Upload audio file to ComfyUI if provided
                string? uploadedAudioFilename = null;
                if (!string.IsNullOrEmpty(config.AudioFilePath))
                {
                    uploadedAudioFilename = await UploadAudioToComfyUIAsync(config.AudioFilePath);
                    if (uploadedAudioFilename == null)
                    {
                        _logger.LogError("Failed to upload audio file {AudioFilePath} to ComfyUI for task {TaskId}", config.AudioFilePath, task.Id);
                        throw new InvalidOperationException($"Failed to upload audio file {config.AudioFilePath} to ComfyUI");
                    }
                    _logger.LogInformation("Successfully uploaded audio {AudioFilePath} to ComfyUI as {UploadedFilename} for task {TaskId}", 
                        config.AudioFilePath, uploadedAudioFilename, task.Id);
                }

                // Step 3: Update the config with uploaded filenames for the workflow
                var workflowConfig = config;
                if (uploadedImageFilename != null || uploadedAudioFilename != null)
                {
                    // Create a copy of the config with the uploaded filenames
                    workflowConfig = new VideoWorkflowConfig
                    {
                        ImageFilePath = uploadedImageFilename ?? config.ImageFilePath,
                        AudioFilePath = uploadedAudioFilename ?? config.AudioFilePath,
                        TextPrompt = config.TextPrompt,
                        DurationSeconds = config.DurationSeconds,
                        Width = config.Width,
                        Height = config.Height,
                        Fps = config.Fps,
                        AnimationStyle = config.AnimationStyle,
                        MotionIntensity = config.MotionIntensity,
                        CheckpointName = config.CheckpointName,
                        Seed = config.Seed,
                        Steps = config.Steps,
                        CFGScale = config.CFGScale,
                        AugmentationLevel = config.AugmentationLevel,
                        OutputFilename = config.OutputFilename,
                        OutputFormat = config.OutputFormat,
                        Quality = config.Quality
                    };
                }

                // Step 4: Create and submit the workflow
                var workflow = VideoWorkflowFactory.CreateWorkflow(workflowConfig);
                var workflowDict = _videoService.ConvertWorkflowToComfyUIFormat(workflow);
                
                _logger.LogDebug("Video workflow prepared for task {TaskId} with {NodeCount} nodes - Duration: {Duration}s, Size: {Width}x{Height}@{Fps}fps", 
                    task.Id, workflowDict.Count, config.DurationSeconds, 
                    config.Width, config.Height, config.Fps);

                if (uploadedImageFilename != null || uploadedAudioFilename != null)
                {
                    _logger.LogInformation("Video workflow for task {TaskId} includes uploaded files - Image: {ImageFile}, Audio: {AudioFile}", 
                        task.Id, uploadedImageFilename ?? "none", uploadedAudioFilename ?? "none");
                }
                
                // Log the workflow being sent (detailed debug info)
                var workflowJson = System.Text.Json.JsonSerializer.Serialize(workflowDict, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Video workflow JSON for task {TaskId}: {WorkflowJson}", task.Id, workflowJson);
                
                var promptId = await _videoService.SubmitWorkflowAsync(workflowDict);
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    _logger.LogInformation("Video workflow submitted successfully for task {TaskId}, received prompt ID: {PromptId}", 
                        task.Id, promptId);
                }
                
                return promptId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting video task {TaskId} to ComfyUI", task.Id);
                throw;
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
                    _logger.LogError("Image file not found: {ImageFilePath} (resolved to: {FullImagePath})", imageFilePath, fullImagePath);
                    return null;
                }

                var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                var filename = Path.GetFileName(fullImagePath);
                
                _logger.LogDebug("Uploading image to ComfyUI: {Filename} ({Size} bytes)", filename, imageBytes.Length);
                
                var uploadResponse = await _comfyUIClient.UploadImageAsync(
                    imageBytes, 
                    filename, 
                    subfolder: "input", // Upload to input subfolder
                    overwrite: true
                );

                _logger.LogDebug("Image upload response: {Response}", System.Text.Json.JsonSerializer.Serialize(uploadResponse));
                return uploadResponse.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image {ImageFilePath} to ComfyUI", imageFilePath);
                return null;
            }
        }

        /// <summary>
        /// Uploads an audio file to ComfyUI and returns the uploaded filename
        /// Note: ComfyUI doesn't have a dedicated audio upload endpoint, so we use the image upload endpoint
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
                    _logger.LogError("Audio file not found: {AudioFilePath} (resolved to: {FullAudioPath})", audioFilePath, fullAudioPath);
                    return null;
                }

                var audioBytes = await File.ReadAllBytesAsync(fullAudioPath);
                var filename = Path.GetFileName(fullAudioPath);
                
                _logger.LogDebug("Uploading audio to ComfyUI: {Filename} ({Size} bytes)", filename, audioBytes.Length);
                
                // Use the image upload endpoint for audio files since ComfyUI doesn't have a dedicated audio upload
                // The file will be stored in the input directory and can be referenced by audio nodes
                var uploadResponse = await _comfyUIClient.UploadImageAsync(
                    audioBytes, 
                    filename, 
                    subfolder: "input", // Upload to input subfolder
                    type: "input", // Mark as input type
                    overwrite: true
                );

                _logger.LogDebug("Audio upload response: {Response}", System.Text.Json.JsonSerializer.Serialize(uploadResponse));
                return uploadResponse.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio {AudioFilePath} to ComfyUI", audioFilePath);
                return null;
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
                _logger.LogError(ex, "Error getting video models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            try
            {
                return await _videoService.GetQueueStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status from ComfyUI video service");
                return null;
            }
        }

        public override async Task<bool> CancelTaskAsync(string promptId)
        {
            try
            {
                return await _videoService.CancelJobAsync(promptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling video task with prompt ID {PromptId}", promptId);
                return false;
            }
        }

        public override async Task<List<string>> GetUNETModelsAsync()
        {
            try
            {
                return await _videoService.GetUNETModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UNET models from ComfyUI");
                return new List<string>();
            }
        }

        public override async Task<List<string>> GetLoRAModelsAsync()
        {
            try
            {
                return await _videoService.GetLoRAModelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LoRA models from ComfyUI");
                return new List<string>();
            }
        }

        protected override async Task<string?> GetGeneratedFileAsync(string promptId)
        {
            try
            {
                return await _videoService.GetGeneratedFileAsync(promptId, OutputSubfolder, FilePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generated video file for prompt ID {PromptId}", promptId);
                return null;
            }
        }
    }
}