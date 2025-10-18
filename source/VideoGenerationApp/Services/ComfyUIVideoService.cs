using System.Text.Json;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;
using ComfyUI.Client.Services;
using VideoGenerationApp.Components;
using System.Runtime.Versioning;
using System.Linq;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Video-specific ComfyUI service for generating videos using SVD (Stable Video Diffusion)
    /// </summary>
    public class ComfyUIVideoService : ComfyUIServiceBase, IComfyUIVideoService
    {
        private readonly IComfyUIFileService _fileService;

        public ComfyUIVideoService(
            IComfyUIApiClient comfyUIClient, 
            ILogger<ComfyUIVideoService> logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings,
            IComfyUIFileService fileService) 
            : base(comfyUIClient, logger, environment, settings)
        {
            _fileService = fileService;
        }


        /// <summary>
        /// Gets the current video workflow template as JSON
        /// </summary>
        public override string GetWorkflowTemplate(string resource = "VideoGenerationApp.Resources.video_example.json")
        {
            return helpers.ResourceReader.ReadEmbeddedJson(resource);
        }

        /// <summary>
        /// Sets the workflow template from JSON
        /// </summary>
        public override void SetWorkflowTemplate(string template)
        {
            // no-op with new builder approach
        }

        /// <summary>
        /// Generates video from VideoSceneOutput (from Ollama)
        /// </summary>
        public override async Task<string?> GenerateAsync(VideoSceneOutput sceneOutput)
        {
            try
            {
                _logger.LogInformation("Generating video from scene output");
                
                var wrapper = new VideoWorkflowWrapper
                {
                    TextPrompt = sceneOutput.visual_description ?? sceneOutput.narrative ?? "",
                    // Use default settings for video generation
                    DurationSeconds = 10,
                    Width = 1024,
                    Height = 1024,
                    Fps = 30,
                    AnimationStyle = "smooth",
                    MotionIntensity = 0.5f
                };
                
                return await GenerateVideoAsync(wrapper);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating video from scene output");
                return null;
            }
        }

        /// <summary>
        /// Generates video using ComfyUI with the provided configuration
        /// </summary>
        public async Task<string?> GenerateVideoAsync(VideoWorkflowWrapper wrapper)
        {
            try
            {
                _logger.LogInformation("Starting video generation with VideoWorkflowWrapper");

                if (!await IsComfyUIRunningAsync())
                {
                    _logger.LogError("ComfyUI is not running. Please start ComfyUI first.");
                    return "NO";
                }

                var task = new VideoGenerationTask(wrapper, this, _comfyUIClient, _environment);
                
                var promptId = await task.SubmitAsync();
                
                if (!string.IsNullOrEmpty(promptId))
                {
                    _logger.LogInformation("Video generation submitted successfully with prompt ID: {PromptId}", promptId);
                    
                    // Wait for completion with extended timeout for video generation
                    var timeout = TimeSpan.FromMinutes(_settings.TimeoutMinutes);
                    _logger.LogInformation("Waiting for video generation to complete (timeout: {Timeout})", timeout);
                    
                    var completed = await WaitForCompletionAsync(promptId, timeout);
                    if (!completed)
                    {
                        _logger.LogError("Video generation timed out after {Timeout}", timeout);
                        return "NO";
                    }

                    return await GetGeneratedFileAsync(promptId, "video", wrapper.OutputFilename);
                }
                
                return "NO";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during video generation");
                return "NO";
            }
        }

        /// <summary>
        /// Converts our workflow DTO to ComfyUI's expected format (basic, without defaults)
        /// </summary>
        public Dictionary<string, object> ConvertWorkflowToComfyUIFormat(ComfyUIAudioWorkflow workflow)
        {
            return new Dictionary<string, object>();
        }


        /// <summary>
        /// Gets the current ComfyUI queue status
        /// </summary>
        public async Task<ComfyUIQueueStatus?> GetQueueStatusAsync()
        {
            try
            {
                var queueResponse = await _comfyUIClient.GetQueueAsync();
                
                var queueStatus = new ComfyUIQueueStatus
                {
                    queue = queueResponse.QueuePending?.Select(q => new ComfyUIQueueItem { prompt_id = q.PromptId }).ToList() ?? new List<ComfyUIQueueItem>(),
                    exec = queueResponse.QueueRunning?.Select(q => new ComfyUIQueueItem { prompt_id = q.PromptId }).ToList() ?? new List<ComfyUIQueueItem>()
                };

                return queueStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status");
                return null;
            }
        }

        /// <summary>
        /// Prepares input files by copying them to ComfyUI's input directory
        /// </summary>
        private async Task<VideoWorkflowWrapper?> PrepareInputFilesForComfyUIAsync(VideoWorkflowWrapper wrapper)
        {
            try
            {
                var prepared = wrapper.Clone();

                if (!string.IsNullOrEmpty(wrapper.ImageFilePath))
                {
                    var copiedImagePath = await _fileService.CopyFileToComfyUIInputAsync(wrapper.ImageFilePath, "image");
                    if (copiedImagePath != null) prepared.ImageFilePath = copiedImagePath; else return null;
                }
                if (!string.IsNullOrEmpty(wrapper.AudioFilePath))
                {
                    var copiedAudioPath = await _fileService.CopyFileToComfyUIInputAsync(wrapper.AudioFilePath, "audio");
                    if (copiedAudioPath != null) prepared.AudioFilePath = copiedAudioPath; else return null;
                }
                return prepared;
            }
            catch (Exception ex)
            {
_logger.LogError(ex, "Error preparing input files");
                return null;
            }
        }
    }
}
