using System.Text.Json;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Audio-specific ComfyUI service for generating audio content
    /// </summary>
    public class ComfyUIAudioService : ComfyUIServiceBase
    {
        private AudioWorkflowConfig _workflowConfig = new();

        public ComfyUIAudioService(
            HttpClient httpClient, 
            ILogger<ComfyUIAudioService> logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings) 
            : base(httpClient, logger, environment, settings)
        {
        }

        /// <summary>
        /// Gets the current audio workflow configuration
        /// </summary>
        public AudioWorkflowConfig GetWorkflowConfig()
        {
            return _workflowConfig;
        }

        /// <summary>
        /// Updates the audio workflow configuration
        /// </summary>
        public void SetWorkflowConfig(AudioWorkflowConfig config)
        {
            _workflowConfig = config;
            _logger.LogInformation("Audio workflow configuration updated");
        }

        /// <summary>
        /// Gets the current audio workflow template as JSON
        /// </summary>
        public override string GetWorkflowTemplate()
        {
            var workflow = AudioWorkflowFactory.CreateWorkflow(_workflowConfig);
            return JsonSerializer.Serialize(workflow, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = null
            });
        }

        /// <summary>
        /// Updates the audio workflow template (for backward compatibility)
        /// </summary>
        public override void SetWorkflowTemplate(string template)
        {
            try
            {
                var workflow = JsonSerializer.Deserialize<ComfyUIAudioWorkflow>(template);
                if (workflow != null)
                {
                    ExtractConfigFromWorkflow(workflow);
                    _logger.LogInformation("Audio workflow template updated from JSON");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse workflow template, using current configuration");
            }
        }

        /// <summary>
        /// Generates audio using ComfyUI with the provided video scene output
        /// </summary>
        public override async Task<string?> GenerateAsync(VideoSceneOutput sceneOutput)
        {
            try
            {
                _logger.LogInformation("Starting audio generation for scene");

                // Check if ComfyUI is running
                if (!await IsComfyUIRunningAsync())
                {
                    _logger.LogError("ComfyUI is not running. Please start ComfyUI first.");
                    return null;
                }

                // Update prompts from scene output
                UpdatePromptsFromScene(sceneOutput);
                _logger.LogInformation("Updated prompts - Positive: '{PositivePrompt}', Negative: '{NegativePrompt}'", 
                    _workflowConfig.PositivePrompt, _workflowConfig.NegativePrompt);
                
                // Create workflow
                var workflow = AudioWorkflowFactory.CreateWorkflow(_workflowConfig);
                var workflowDict = ConvertWorkflowToComfyUIFormat(workflow);

                // Log workflow for debugging
                var workflowJson = JsonSerializer.Serialize(workflowDict, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Generated workflow JSON: {WorkflowJson}", workflowJson);

                // Submit workflow
                var promptId = await SubmitWorkflowAsync(workflowDict);
                if (string.IsNullOrEmpty(promptId))
                {
                    _logger.LogError("Failed to submit audio generation workflow");
                    return null;
                }

                // Wait for completion with extended timeout for long audio generation
                var timeout = TimeSpan.FromMinutes(_settings.TimeoutMinutes);
                _logger.LogInformation("Waiting for audio generation to complete (timeout: {Timeout})", timeout);
                
                var completed = await WaitForCompletionAsync(promptId, timeout);
                if (!completed)
                {
                    _logger.LogError("Audio generation timed out after {Timeout}", timeout);
                    return null;
                }

                // Get generated audio file
                return await GetGeneratedFileAsync(promptId, "audio", _workflowConfig.FilenamePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audio generation");
                return null;
            }
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

                var queueJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ComfyUIQueueStatus>(queueJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting queue status");
                return null;
            }
        }

        /// <summary>
        /// Generates audio using ComfyUI with the provided video scene output (alias for GenerateAsync)
        /// </summary>
        public async Task<string?> GenerateAudioAsync(VideoSceneOutput sceneOutput)
        {
            return await GenerateAsync(sceneOutput);
        }

        /// <summary>
        /// Updates prompts in the workflow config based on scene output
        /// </summary>
        private void UpdatePromptsFromScene(VideoSceneOutput sceneOutput)
        {
            var promptParts = new List<string>();

            // Add audio-specific information from scene
            if (sceneOutput.audio != null)
            {
                if (!string.IsNullOrEmpty(sceneOutput.audio.background_music))
                    promptParts.Add(sceneOutput.audio.background_music);

                if (!string.IsNullOrEmpty(sceneOutput.audio.audio_mood))
                    promptParts.Add(sceneOutput.audio.audio_mood);

                if (sceneOutput.audio.sound_effects?.Any() == true)
                    promptParts.Add(string.Join(" ", sceneOutput.audio.sound_effects));
            }

            // Add narrative context if no specific audio instructions
            if (promptParts.Count == 0)
            {
                if (!string.IsNullOrEmpty(sceneOutput.tone))
                    promptParts.Add(sceneOutput.tone);
                if (!string.IsNullOrEmpty(sceneOutput.emotion))
                    promptParts.Add(sceneOutput.emotion);
                promptParts.Add("music");
            }

            _workflowConfig.PositivePrompt = string.Join(" ", promptParts);
            
            // Ensure negative prompt is not empty (ComfyUI requires text input for CLIPTextEncode)
            if (string.IsNullOrEmpty(_workflowConfig.NegativePrompt))
            {
                _workflowConfig.NegativePrompt = "bad quality, distorted, noise";
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

                // Then, add widget values based on the specific node type patterns from audio_example.json
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
                    
                    case "CLIPLoader":
                        // CLIPLoader widgets: [clip_name, type, device]
                        if (node.widgets_values.Length >= 3)
                        {
                            inputs["clip_name"] = node.widgets_values[0];
                            inputs["type"] = node.widgets_values[1];
                            inputs["device"] = node.widgets_values[2];
                        }
                        break;
                    
                    case "CLIPTextEncode":
                        // CLIPTextEncode widgets: [text]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["text"] = node.widgets_values[0];
                        }
                        break;
                    
                    case "CheckpointLoaderSimple":
                        // CheckpointLoaderSimple widgets: [ckpt_name]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["ckpt_name"] = node.widgets_values[0];
                        }
                        break;
                    
                    case "EmptyLatentAudio":
                        // EmptyLatentAudio widgets: [seconds, batch_size]
                        if (node.widgets_values.Length >= 2)
                        {
                            inputs["seconds"] = node.widgets_values[0];
                            inputs["batch_size"] = node.widgets_values[1];
                        }
                        break;
                    
                    case "SaveAudio":
                        // SaveAudio widgets: [filename_prefix]
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
            if (kSamplerNode?.widgets_values?.Length >= 6)
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

            var positiveTextNode = workflow.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 6);
            if (positiveTextNode?.widgets_values?.Length > 0 && positiveTextNode.widgets_values[0] is string positivePrompt)
            {
                _workflowConfig.PositivePrompt = positivePrompt;
            }

            var negativeTextNode = workflow.nodes.FirstOrDefault(n => n.type == "CLIPTextEncode" && n.id == 7);
            if (negativeTextNode?.widgets_values?.Length > 0 && negativeTextNode.widgets_values[0] is string negativePrompt)
            {
                _workflowConfig.NegativePrompt = negativePrompt;
            }

            var audioLatentNode = workflow.nodes.FirstOrDefault(n => n.type == "EmptyLatentAudio");
            if (audioLatentNode?.widgets_values?.Length >= 2)
            {
                if (float.TryParse(audioLatentNode.widgets_values[0]?.ToString(), out float duration))
                    _workflowConfig.AudioDurationSeconds = duration;
                if (int.TryParse(audioLatentNode.widgets_values[1]?.ToString(), out int batchSize))
                    _workflowConfig.BatchSize = batchSize;
            }
        }
    }
}