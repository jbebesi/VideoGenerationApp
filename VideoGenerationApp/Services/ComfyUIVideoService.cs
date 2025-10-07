using System.Text.Json;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Video-specific ComfyUI service for generating videos using SVD (Stable Video Diffusion)
    /// </summary>
    public class ComfyUIVideoService : ComfyUIServiceBase
    {
        private VideoWorkflowConfig _workflowConfig = new();

        public ComfyUIVideoService(
            HttpClient httpClient, 
            ILogger<ComfyUIVideoService> logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings) 
            : base(httpClient, logger, environment, settings)
        {
        }

        /// <summary>
        /// Gets the current video workflow configuration
        /// </summary>
        public virtual VideoWorkflowConfig GetWorkflowConfig()
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
                
                var workflow = VideoWorkflowFactory.CreateWorkflow(config);
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
                            inputs[input.name] = new object[] { sourceNodeId.ToString(), sourceOutputIndex };
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
                        // SVD_img2vid_Conditioning widgets: [width, height, video_frames, motion_bucket_id, augmentation_level, seed]
                        if (node.widgets_values.Length >= 6)
                        {
                            inputs["width"] = node.widgets_values[0];
                            inputs["height"] = node.widgets_values[1];
                            inputs["video_frames"] = node.widgets_values[2];
                            inputs["motion_bucket_id"] = node.widgets_values[3];
                            inputs["augmentation_level"] = node.widgets_values[4];
                            inputs["seed"] = node.widgets_values[5];
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

                    case "VHS_VideoCombine":
                        // VHS_VideoCombine widgets: [frame_rate, loop_count, filename_prefix, format, pingpong, save_image, audio, quality]
                        if (node.widgets_values.Length >= 8)
                        {
                            inputs["frame_rate"] = node.widgets_values[0];
                            inputs["loop_count"] = node.widgets_values[1];
                            inputs["filename_prefix"] = node.widgets_values[2];
                            inputs["format"] = node.widgets_values[3];
                            inputs["pingpong"] = node.widgets_values[4];
                            inputs["save_image"] = node.widgets_values[5];
                            if (node.widgets_values[6] != null)
                            {
                                inputs["audio"] = node.widgets_values[6];
                            }
                            inputs["videoformat"] = node.widgets_values[3]; // format is also called videoformat in some versions
                            inputs["crf"] = node.widgets_values[7]; // quality/CRF
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

                    case "VHS_VideoCombine":
                        if (node.widgets_values.Length >= 8)
                        {
                            if (node.widgets_values[0] is int fps) config.Fps = fps;
                            if (node.widgets_values[2] is string filename) config.OutputFilename = filename;
                            if (node.widgets_values[3] is string format) config.OutputFormat = format;
                            if (node.widgets_values[7] is int quality) config.Quality = quality;
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
                var response = await _httpClient.GetAsync("/queue");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get queue status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var queueStatus = JsonSerializer.Deserialize<ComfyUIQueueStatus>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return queueStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status");
                return null;
            }
        }
    }
}
