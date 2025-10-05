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

                // Update tags and lyrics from scene output
                UpdatePromptsFromScene(sceneOutput);
                _logger.LogInformation("Updated ACE Step config - Tags: '{Tags}', Lyrics: '{Lyrics}'", 
                    _workflowConfig.Tags, _workflowConfig.Lyrics?.Substring(0, Math.Min(50, _workflowConfig.Lyrics?.Length ?? 0)));
                
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
                return await GetGeneratedFileAsync(promptId, "audio", _workflowConfig.OutputFilename);
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
        /// Updates ACE Step configuration from scene output
        /// </summary>
        private void UpdatePromptsFromScene(VideoSceneOutput sceneOutput)
        {
            var tagParts = new List<string>();

            // Add emotional context and tone
            if (!string.IsNullOrEmpty(sceneOutput.tone))
                tagParts.Add(sceneOutput.tone);
            if (!string.IsNullOrEmpty(sceneOutput.emotion))
                tagParts.Add(sceneOutput.emotion);

            // Add audio-specific information from scene
            if (sceneOutput.audio != null)
            {
                if (!string.IsNullOrEmpty(sceneOutput.audio.background_music))
                    tagParts.Add(sceneOutput.audio.background_music);

                if (!string.IsNullOrEmpty(sceneOutput.audio.audio_mood))
                    tagParts.Add(sceneOutput.audio.audio_mood);
            }

            // Update tags if we have any content
            if (tagParts.Count > 0)
            {
                _workflowConfig.Tags = string.Join(", ", tagParts);
            }

            // Create basic lyrics from narrative if available
            if (!string.IsNullOrEmpty(sceneOutput.narrative))
            {
                var narrative = sceneOutput.narrative;
                var truncatedNarrative = narrative.Length > 100 
                    ? narrative.Substring(0, 100) + "..." 
                    : narrative;
                
                _workflowConfig.Lyrics = $"[verse]\n{truncatedNarrative}\n[chorus]\nSing with me tonight\nEverything will be alright";
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

                    case "EmptyAceStepLatentAudio":
                        // EmptyAceStepLatentAudio widgets: [seconds, batch_size]
                        if (node.widgets_values.Length >= 2)
                        {
                            inputs["seconds"] = node.widgets_values[0];
                            inputs["batch_size"] = node.widgets_values[1];
                        }
                        break;

                    case "TextEncodeAceStepAudio":
                        // TextEncodeAceStepAudio widgets: [tags, lyrics, lyrics_strength]
                        if (node.widgets_values.Length >= 3)
                        {
                            inputs["tags"] = node.widgets_values[0];
                            inputs["lyrics"] = node.widgets_values[1];
                            inputs["lyrics_strength"] = node.widgets_values[2];
                        }
                        break;

                    case "ModelSamplingSD3":
                        // ModelSamplingSD3 widgets: [shift]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["shift"] = node.widgets_values[0];
                        }
                        break;

                    case "LatentOperationTonemapReinhard":
                        // LatentOperationTonemapReinhard widgets: [multiplier]
                        if (node.widgets_values.Length >= 1)
                        {
                            inputs["multiplier"] = node.widgets_values[0];
                        }
                        break;

                    case "SaveAudioMP3":
                        // SaveAudioMP3 widgets: [filename_prefix, quality]
                        if (node.widgets_values.Length >= 2)
                        {
                            inputs["filename_prefix"] = node.widgets_values[0];
                            inputs["quality"] = node.widgets_values[1];
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

            // Extract ACE Step text encoding parameters
            var aceStepTextNode = workflow.nodes.FirstOrDefault(n => n.type == "TextEncodeAceStepAudio");
            if (aceStepTextNode?.widgets_values?.Length >= 3)
            {
                if (aceStepTextNode.widgets_values[0] is string tags)
                    _workflowConfig.Tags = tags;
                if (aceStepTextNode.widgets_values[1] is string lyrics)
                    _workflowConfig.Lyrics = lyrics;
                if (float.TryParse(aceStepTextNode.widgets_values[2]?.ToString(), out float lyricsStrength))
                    _workflowConfig.LyricsStrength = lyricsStrength;
            }

            // Extract ACE Step latent audio parameters
            var aceStepLatentNode = workflow.nodes.FirstOrDefault(n => n.type == "EmptyAceStepLatentAudio");
            if (aceStepLatentNode?.widgets_values?.Length >= 2)
            {
                if (float.TryParse(aceStepLatentNode.widgets_values[0]?.ToString(), out float duration))
                    _workflowConfig.AudioDurationSeconds = duration;
                if (int.TryParse(aceStepLatentNode.widgets_values[1]?.ToString(), out int batchSize))
                    _workflowConfig.BatchSize = batchSize;
            }

            // Extract model shift parameter
            var modelSamplingNode = workflow.nodes.FirstOrDefault(n => n.type == "ModelSamplingSD3");
            if (modelSamplingNode?.widgets_values?.Length > 0)
            {
                if (float.TryParse(modelSamplingNode.widgets_values[0]?.ToString(), out float modelShift))
                    _workflowConfig.ModelShift = modelShift;
            }

            // Extract tonemap multiplier
            var tonemapNode = workflow.nodes.FirstOrDefault(n => n.type == "LatentOperationTonemapReinhard");
            if (tonemapNode?.widgets_values?.Length > 0)
            {
                if (float.TryParse(tonemapNode.widgets_values[0]?.ToString(), out float tonemapMultiplier))
                    _workflowConfig.TonemapMultiplier = tonemapMultiplier;
            }
        }
    }
}