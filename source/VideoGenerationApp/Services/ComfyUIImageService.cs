using System.Text.Json;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Image-specific ComfyUI service for generating images
    /// </summary>
    public class ComfyUIImageService : ComfyUIServiceBase, IComfyUIImageService
    {
        private ImageWorkflowConfig _workflowConfig = new();

        public ComfyUIImageService(
            IComfyUIApiClient comfyUIClient, 
            ILogger<ComfyUIImageService> logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings) 
            : base(comfyUIClient, logger, environment, settings)
        {
        }

        /// <summary>
        /// Gets the current image workflow configuration
        /// </summary>
        public ImageWorkflowConfig GetWorkflowConfig()
        {
            return _workflowConfig;
        }

        /// <summary>
        /// Updates the image workflow configuration
        /// </summary>
        public void SetWorkflowConfig(ImageWorkflowConfig config)
        {
            _workflowConfig = config;
            _logger.LogInformation("Image workflow configuration updated");
        }

        /// <summary>
        /// Gets the current image workflow template as JSON
        /// </summary>
        public override string GetWorkflowTemplate()
        {
            var workflow = ImageWorkflowFactory.CreateWorkflow(_workflowConfig);
            return JsonSerializer.Serialize(workflow, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = null
            });
        }

        /// <summary>
        /// Updates the image workflow template (for backward compatibility)
        /// </summary>
        public override void SetWorkflowTemplate(string template)
        {
            try
            {
                var workflow = JsonSerializer.Deserialize<ComfyUIAudioWorkflow>(template);
                if (workflow != null)
                {
                    ExtractConfigFromWorkflow(workflow);
                    _logger.LogInformation("Image workflow template updated from JSON");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse workflow template, using current configuration");
            }
        }

        /// <summary>
        /// Generates image using ComfyUI with the provided video scene output
        /// </summary>
        public override async Task<string?> GenerateAsync(VideoSceneOutput sceneOutput)
        {
            try
            {
                _logger.LogInformation("Starting image generation for scene");

                // Check if ComfyUI is running
                if (!await IsComfyUIRunningAsync())
                {
                    _logger.LogError("ComfyUI is not running. Please start ComfyUI first.");
                    return null;
                }

                // Update prompts from scene output
                UpdatePromptsFromScene(sceneOutput);
                _logger.LogInformation("Updated image config - Positive: '{Positive}', Negative: '{Negative}'", 
                    _workflowConfig.PositivePrompt?.Substring(0, Math.Min(50, _workflowConfig.PositivePrompt?.Length ?? 0)),
                    _workflowConfig.NegativePrompt?.Substring(0, Math.Min(30, _workflowConfig.NegativePrompt?.Length ?? 0)));
                
                // Create workflow
                var workflow = ImageWorkflowFactory.CreateWorkflow(_workflowConfig);
                var workflowDict = ConvertWorkflowToComfyUIFormat(workflow);

                // Log workflow for debugging
                var workflowJson = JsonSerializer.Serialize(workflowDict, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Submitting image workflow to ComfyUI");
                _logger.LogDebug("Generated workflow JSON: {WorkflowJson}", workflowJson);

                // Submit workflow
                var promptId = await SubmitWorkflowAsync(workflowDict);
                if (string.IsNullOrEmpty(promptId))
                {
                    _logger.LogError("Failed to submit image generation workflow");
                    return null;
                }

                _logger.LogInformation("Image workflow submitted successfully with prompt ID: {PromptId}", promptId);

                // Wait for completion with extended timeout for image generation
                var timeout = TimeSpan.FromMinutes(_settings.TimeoutMinutes);
                _logger.LogInformation("Waiting for image generation to complete (timeout: {Timeout})", timeout);
                
                var completed = await WaitForCompletionAsync(promptId, timeout);
                if (!completed)
                {
                    _logger.LogError("Image generation timed out after {Timeout}", timeout);
                    return null;
                }

                // Get generated image file
                return await GetGeneratedFileAsync(promptId, "image", _workflowConfig.OutputFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image generation");
                return null;
            }
        }

        /// <summary>
        /// Generates image using ComfyUI with the provided configuration
        /// </summary>
        public async Task<string?> GenerateImageAsync(ImageWorkflowConfig config)
        {
            _workflowConfig = config;
            
            try
            {
                _logger.LogInformation("Starting image generation with custom config");

                // Check if ComfyUI is running
                if (!await IsComfyUIRunningAsync())
                {
                    _logger.LogError("ComfyUI is not running. Please start ComfyUI first.");
                    return null;
                }

                _logger.LogInformation("Image config - Positive: '{Positive}', Negative: '{Negative}', Size: {Width}x{Height}, Steps: {Steps}", 
                    _workflowConfig.PositivePrompt?.Substring(0, Math.Min(50, _workflowConfig.PositivePrompt?.Length ?? 0)),
                    _workflowConfig.NegativePrompt?.Substring(0, Math.Min(30, _workflowConfig.NegativePrompt?.Length ?? 0)),
                    _workflowConfig.Width,
                    _workflowConfig.Height,
                    _workflowConfig.Steps);
                
                // Create workflow
                var workflow = ImageWorkflowFactory.CreateWorkflow(_workflowConfig);
                var workflowDict = ConvertWorkflowToComfyUIFormat(workflow);

                // Log workflow for debugging
                var workflowJson = JsonSerializer.Serialize(workflowDict, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Submitting image workflow to ComfyUI");
                _logger.LogDebug("Generated workflow JSON: {WorkflowJson}", workflowJson);

                // Submit workflow
                var promptId = await SubmitWorkflowAsync(workflowDict);
                if (string.IsNullOrEmpty(promptId))
                {
                    _logger.LogError("Failed to submit image generation workflow");
                    return null;
                }

                _logger.LogInformation("Image workflow submitted successfully with prompt ID: {PromptId}", promptId);

                // Get generated image file
                return await GetGeneratedFileAsync(promptId, "image", _workflowConfig.OutputFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image generation");
                return null;
            }
        }

        /// <summary>
        /// Updates image configuration from scene output
        /// </summary>
        private void UpdatePromptsFromScene(VideoSceneOutput sceneOutput)
        {
            // Use visual description as positive prompt
            if (!string.IsNullOrEmpty(sceneOutput.visual_description))
            {
                _workflowConfig.PositivePrompt = sceneOutput.visual_description;
            }

            // Add tone and emotion to enhance the prompt
            if (!string.IsNullOrEmpty(sceneOutput.tone) || !string.IsNullOrEmpty(sceneOutput.emotion))
            {
                var enhancedPrompt = _workflowConfig.PositivePrompt;
                if (!string.IsNullOrEmpty(sceneOutput.tone))
                    enhancedPrompt += $", {sceneOutput.tone} atmosphere";
                if (!string.IsNullOrEmpty(sceneOutput.emotion))
                    enhancedPrompt += $", {sceneOutput.emotion} mood";
                    
                _workflowConfig.PositivePrompt = enhancedPrompt;
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

                // First, add all linked inputs (connections between nodes)
                foreach (var input in node.inputs.Where(i => i.link.HasValue))
                {
                    var sourceNode = workflow.nodes.FirstOrDefault(n => 
                        n.outputs.Any(o => o.links.Contains(input.link!.Value)));
                    if (sourceNode != null)
                    {
                        var outputIndex = sourceNode.outputs.FindIndex(o => o.links.Contains(input.link!.Value));
                        inputs[input.name] = new object[] { sourceNode.id.ToString(), outputIndex };
                    }
                }

                // Then, add widget values based on the specific node type
                switch (node.type)
                {
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
                    
                    case "CheckpointLoaderSimple":
                        // CheckpointLoaderSimple widgets: [ckpt_name]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["ckpt_name"] = node.widgets_values[0];
                        }
                        break;
                    
                    case "EmptyLatentImage":
                        // EmptyLatentImage widgets: [width, height, batch_size]
                        if (node.widgets_values.Length >= 3)
                        {
                            inputs["width"] = node.widgets_values[0];
                            inputs["height"] = node.widgets_values[1];
                            inputs["batch_size"] = node.widgets_values[2];
                        }
                        break;

                    case "CLIPTextEncode":
                        // CLIPTextEncode widgets: [text]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["text"] = node.widgets_values[0];
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
            var kSamplerNode = workflow.nodes.FirstOrDefault(n => n.type == "KSampler");
            if (kSamplerNode?.widgets_values?.Length >= 7)
            {
                if (long.TryParse(kSamplerNode.widgets_values[0]?.ToString(), out long seed))
                    _workflowConfig.Seed = seed;
                if (int.TryParse(kSamplerNode.widgets_values[2]?.ToString(), out int steps))
                    _workflowConfig.Steps = steps;
                if (float.TryParse(kSamplerNode.widgets_values[3]?.ToString(), out float cfg))
                    _workflowConfig.CFGScale = cfg;
                if (kSamplerNode.widgets_values[4] is string samplerName)
                    _workflowConfig.SamplerName = samplerName;
                if (kSamplerNode.widgets_values[5] is string scheduler)
                    _workflowConfig.Scheduler = scheduler;
                if (float.TryParse(kSamplerNode.widgets_values[6]?.ToString(), out float denoise))
                    _workflowConfig.Denoise = denoise;
            }

            // Extract positive prompt
            var positiveTextNode = workflow.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 6);
            if (positiveTextNode?.widgets_values?.Length >= 1)
            {
                if (positiveTextNode.widgets_values[0] is string positivePrompt)
                    _workflowConfig.PositivePrompt = positivePrompt;
            }

            // Extract negative prompt
            var negativeTextNode = workflow.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 7);
            if (negativeTextNode?.widgets_values?.Length >= 1)
            {
                if (negativeTextNode.widgets_values[0] is string negativePrompt)
                    _workflowConfig.NegativePrompt = negativePrompt;
            }

            // Extract image dimensions
            var latentImageNode = workflow.nodes.FirstOrDefault(n => n.type == "EmptyLatentImage");
            if (latentImageNode?.widgets_values?.Length >= 3)
            {
                if (int.TryParse(latentImageNode.widgets_values[0]?.ToString(), out int width))
                    _workflowConfig.Width = width;
                if (int.TryParse(latentImageNode.widgets_values[1]?.ToString(), out int height))
                    _workflowConfig.Height = height;
                if (int.TryParse(latentImageNode.widgets_values[2]?.ToString(), out int batchSize))
                    _workflowConfig.BatchSize = batchSize;
            }

            // Extract checkpoint name
            var checkpointNode = workflow.nodes.FirstOrDefault(n => n.type == "CheckpointLoaderSimple");
            if (checkpointNode?.widgets_values?.Length >= 1)
            {
                if (checkpointNode.widgets_values[0] is string checkpointName)
                    _workflowConfig.CheckpointName = checkpointName;
            }
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
                _logger.LogWarning(ex, "Error getting queue status");
                return null;
            }
        }
    }
}
