using System.Text.Json;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Video-specific ComfyUI service for generating videos using SVD (Stable Video Diffusion)
    /// </summary>
    public class ComfyUIVideoService : ComfyUIServiceBase, IComfyUIVideoService
    {
        private VideoWorkflowConfig _workflowConfig = new();
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
        /// Gets the current video workflow configuration
        /// </summary>
        public VideoWorkflowConfig GetWorkflowConfig()
        {
            return _workflowConfig;
        }

        /// <summary>
        /// Updates the video workflow configuration
        /// </summary>
        public void SetWorkflowConfig(VideoWorkflowConfig config)
        {
            _workflowConfig = config;
            _logger.LogInformation("Video workflow configuration updated");
        }

        /// <summary>
        /// Gets the current video workflow template as JSON
        /// </summary>
        public override string GetWorkflowTemplate()
        {
            var workflow = VideoWorkflowFactory.CreateWorkflow(_workflowConfig);
            return JsonSerializer.Serialize(workflow, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = null
            });
        }

        /// <summary>
        /// Sets the workflow template from JSON (for backward compatibility)
        /// </summary>
        public override void SetWorkflowTemplate(string template)
        {
            try
            {
                var workflow = JsonSerializer.Deserialize<ComfyUIAudioWorkflow>(template);
                if (workflow != null)
                {
                    ExtractConfigFromWorkflow(workflow);
                    _logger.LogInformation("Video workflow template updated from JSON");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting video workflow template from JSON");
            }
        }

        /// <summary>
        /// Generates video from VideoSceneOutput (from Ollama)
        /// </summary>
        public override async Task<string?> GenerateAsync(VideoSceneOutput sceneOutput)
        {
            try
            {
                _logger.LogInformation("Generating video from scene output");
                
                var config = new VideoWorkflowConfig
                {
                    TextPrompt = sceneOutput.visual_description ?? sceneOutput.narrative ?? "",
                    // Use default settings for video generation
                    DurationSeconds = 10.0f,
                    Width = 1024,
                    Height = 1024,
                    Fps = 30,
                    AnimationStyle = "smooth",
                    MotionIntensity = 0.5f
                };
                
                return await GenerateVideoAsync(config);
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
        public async Task<string?> GenerateVideoAsync(VideoWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Starting video generation with ComfyUI");
                SetWorkflowConfig(config);
                
                // Copy input files to ComfyUI input directory before generating video
                var preparedConfig = await PrepareInputFilesForComfyUIAsync(config);
                if (preparedConfig == null)
                {
                    _logger.LogError("Failed to prepare input files for ComfyUI");
                    return null;
                }
                
                var workflow = VideoWorkflowFactory.CreateWorkflow(preparedConfig);
                var workflowDict = ConvertWorkflowToComfyUIFormat(workflow);
                
                var promptId = await SubmitWorkflowAsync(workflowDict);
                
                if (string.IsNullOrEmpty(promptId))
                {
                    _logger.LogError("Failed to submit video workflow to ComfyUI");
                    return null;
                }
                
                _logger.LogInformation("Video workflow submitted successfully with prompt ID: {PromptId}", promptId);
                return promptId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating video");
                return null;
            }
        }

        /// <summary>
        /// Converts our workflow DTO to ComfyUI's expected format
        /// </summary>
        public Dictionary<string, object> ConvertWorkflowToComfyUIFormat(ComfyUIAudioWorkflow workflow)
        {
            var result = new Dictionary<string, object>();

            foreach (var node in workflow.nodes)
            {
                var nodeData = new Dictionary<string, object>
                {
                    ["class_type"] = node.type,
                    ["inputs"] = new Dictionary<string, object>()
                };

                var inputs = (Dictionary<string, object>)nodeData["inputs"];

                // Add connected inputs
                foreach (var input in node.inputs)
                {
                    if (input.link.HasValue)
                    {
                        // Find the link
                        var link = workflow.links.FirstOrDefault(l => 
                            l is object[] arr && arr.Length > 0 && arr[0] is int linkId && linkId == input.link.Value);
                        
                        if (link is object[] linkArray && linkArray.Length >= 6)
                        {
                            var sourceNodeId = linkArray[1];
                            var sourceOutputIndex = linkArray[2];
                            inputs[input.name] = new object[] { sourceNodeId?.ToString() ?? "", sourceOutputIndex };
                        }
                    }
                }

                // Add widget values to inputs based on node type
                switch (node.type)
                {
                    case "ImageOnlyCheckpointLoader":
                        // ImageOnlyCheckpointLoader widgets: [ckpt_name]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["ckpt_name"] = node.widgets_values[0];
                        }
                        break;

                    case "LoadImage":
                        // LoadImage widgets: [image]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["image"] = node.widgets_values[0];
                        }
                        break;

                    case "SVD_img2vid_Conditioning":
                        // SVD_img2vid_Conditioning widgets: [width, height, video_frames, motion_bucket_id, fps, augmentation_level, seed]
                        if (node.widgets_values.Length >= 7)
                        {
                            inputs["width"] = node.widgets_values[0];
                            inputs["height"] = node.widgets_values[1];
                            inputs["video_frames"] = node.widgets_values[2];
                            inputs["motion_bucket_id"] = node.widgets_values[3];
                            inputs["fps"] = node.widgets_values[4];
                            inputs["augmentation_level"] = node.widgets_values[5];
                            inputs["seed"] = node.widgets_values[6];
                        }
                        break;

                    case "KSampler":
                        // KSampler widgets: [seed, control_after_generate, steps, cfg, sampler_name, scheduler, denoise]
                        if (node.widgets_values.Length >= 7)
                        {
                            inputs["seed"] = node.widgets_values[0];
                            inputs["steps"] = node.widgets_values[2];
                            inputs["cfg"] = node.widgets_values[3];
                            inputs["sampler_name"] = node.widgets_values[4];
                            inputs["scheduler"] = node.widgets_values[5];
                            inputs["denoise"] = node.widgets_values[6];
                        }
                        break;

                    case "SaveImage":
                        // SaveImage widgets: [filename_prefix]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["filename_prefix"] = node.widgets_values[0];
                        }
                        break;
                }

                result[node.id.ToString()] = nodeData;
            }

            return result;
        }

        /// <summary>
        /// Extracts configuration from a workflow (for backward compatibility)
        /// </summary>
        private void ExtractConfigFromWorkflow(ComfyUIAudioWorkflow workflow)
        {
            // Extract key parameters from workflow nodes
            var config = new VideoWorkflowConfig();

            foreach (var node in workflow.nodes)
            {
                switch (node.type)
                {
                    case "ImageOnlyCheckpointLoader":
                        if (node.widgets_values.Length >= 1 && node.widgets_values[0] is string ckptName)
                        {
                            config.CheckpointName = ckptName;
                        }
                        break;

                    case "SVD_img2vid_Conditioning":
                        if (node.widgets_values.Length >= 6)
                        {
                            if (node.widgets_values[0] is int width) config.Width = width;
                            if (node.widgets_values[1] is int height) config.Height = height;
                            if (node.widgets_values[3] is int motionId)
                            {
                                config.MotionIntensity = (motionId - 127) / 127.0f;
                            }
                            if (node.widgets_values[5] is long seed) config.Seed = seed;
                        }
                        break;

                    case "KSampler":
                        if (node.widgets_values.Length >= 4)
                        {
                            if (node.widgets_values[2] is int steps) config.Steps = steps;
                            if (node.widgets_values[3] is float cfgFloat)
                            {
                                config.CFGScale = cfgFloat;
                            }
                            else if (node.widgets_values[3] is double cfgDouble)
                            {
                                config.CFGScale = (float)cfgDouble;
                            }
                        }
                        break;

                    case "SaveImage":
                        if (node.widgets_values.Length >= 1)
                        {
                            if (node.widgets_values[0] is string filename) config.OutputFilename = filename;
                        }
                        break;
                }
            }

            _workflowConfig = config;
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
        private async Task<VideoWorkflowConfig?> PrepareInputFilesForComfyUIAsync(VideoWorkflowConfig config)
        {
            try
            {
                _logger.LogInformation("Preparing input files for ComfyUI video generation");
                
                // Create a copy of the config to modify
                var preparedConfig = new VideoWorkflowConfig
                {
                    AudioFilePath = config.AudioFilePath,
                    ImageFilePath = config.ImageFilePath,
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

                // Copy image file if provided
                if (!string.IsNullOrEmpty(config.ImageFilePath))
                {
                    var copiedImagePath = await _fileService.CopyFileToComfyUIInputAsync(config.ImageFilePath, "image");
                    if (copiedImagePath != null)
                    {
                        preparedConfig.ImageFilePath = copiedImagePath;
                        _logger.LogInformation("Image file copied to ComfyUI input: {ImagePath}", copiedImagePath);
                    }
                    else
                    {
                        _logger.LogError("Failed to copy image file to ComfyUI input");
                        return null;
                    }
                }

                // Copy audio file if provided
                if (!string.IsNullOrEmpty(config.AudioFilePath))
                {
                    var copiedAudioPath = await _fileService.CopyFileToComfyUIInputAsync(config.AudioFilePath, "audio");
                    if (copiedAudioPath != null)
                    {
                        preparedConfig.AudioFilePath = copiedAudioPath;
                        _logger.LogInformation("Audio file copied to ComfyUI input: {AudioPath}", copiedAudioPath);
                    }
                    else
                    {
                        _logger.LogError("Failed to copy audio file to ComfyUI input");
                        return null;
                    }
                }

                _logger.LogInformation("Successfully prepared input files for ComfyUI");
                return preparedConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing input files for ComfyUI");
                return null;
            }
        }
    }
}
