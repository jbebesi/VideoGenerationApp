using System.Text.Json;
using System.Text;
using System.Linq;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;
using ComfyUI.Client.Services;
using ComfyUI.Client.Models.Requests;
using ComfyUI.Client.Models.Responses;
using FluentValidation;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Abstract base class for ComfyUI services that provides common functionality
    /// for different ComfyUI use cases (audio, video, image generation, etc.)
    /// </summary>
    public abstract class ComfyUIServiceBase : IDisposable
    {
        protected readonly IComfyUIApiClient _comfyUIClient;
        protected readonly ILogger _logger;
        protected readonly IWebHostEnvironment _environment;
        protected readonly ComfyUISettings _settings;

        protected ComfyUIServiceBase(
            IComfyUIApiClient comfyUIClient, 
            ILogger logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings)
        {
            _comfyUIClient = comfyUIClient;
            _logger = logger;
            _environment = environment;
            _settings = settings?.Value ?? new ComfyUISettings 
            {
                ApiUrl = "http://localhost:8188",
                TimeoutMinutes = 10,
                PollIntervalSeconds = 2
            };
        }



        /// <summary>
        /// Checks if ComfyUI is running and responding to API calls
        /// </summary>
        public virtual async Task<bool> IsComfyUIRunningAsync()
        {
            try
            {
                await _comfyUIClient.GetSystemStatsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all available models (checkpoints) from ComfyUI for a specific node type
        /// </summary>
        /// <param name="nodeType">The node type to query (e.g., "CheckpointLoaderSimple", "ImageOnlyCheckpointLoader")</param>
        /// <returns>List of available model names</returns>
        public virtual async Task<List<string>> GetAvailableModelsAsync(string nodeType)
        {
            try
            {
                _logger.LogInformation("Fetching available models for node type: {NodeType}", nodeType);
                
                // Get all object info from ComfyUI
                var objectInfo = await _comfyUIClient.GetObjectInfoAsync();
                
                // Navigate to the specific node type in the object_info response
                if (!objectInfo.TryGetValue(nodeType, out var nodeInfoValue))
                {
                    _logger.LogWarning("Node type {NodeType} not found in ComfyUI object_info", nodeType);
                    return new List<string>();
                }

                var nodeInfoElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeInfoValue));

                // Get the input types for this node
                if (!nodeInfoElement.TryGetProperty("input", out var inputInfo))
                {
                    _logger.LogWarning("No input info found for node type {NodeType}", nodeType);
                    return new List<string>();
                }

                // Look for the required inputs which contain the model/checkpoint options
                if (!inputInfo.TryGetProperty("required", out var requiredInputs))
                {
                    _logger.LogWarning("No required inputs found for node type {NodeType}", nodeType);
                    return new List<string>();
                }

                // Find the checkpoint name field (usually "ckpt_name" or similar)
                var checkpointFieldNames = new[] { "ckpt_name", "checkpoint", "model_name" };
                foreach (var fieldName in checkpointFieldNames)
                {
                    if (requiredInputs.TryGetProperty(fieldName, out var checkpointField))
                    {
                        // The field value is an array where the first element contains the available options
                        if (checkpointField.ValueKind == JsonValueKind.Array && checkpointField.GetArrayLength() > 0)
                        {
                            var optionsElement = checkpointField[0];
                            if (optionsElement.ValueKind == JsonValueKind.Array)
                            {
                                var models = new List<string>();
                                foreach (var model in optionsElement.EnumerateArray())
                                {
                                    if (model.ValueKind == JsonValueKind.String)
                                    {
                                        models.Add(model.GetString()!);
                                    }
                                }
                                
                                _logger.LogInformation("Found {Count} models for node type {NodeType}", models.Count, nodeType);
                                return models;
                            }
                        }
                    }
                }

                _logger.LogWarning("No checkpoint field found in required inputs for node type {NodeType}", nodeType);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available models from ComfyUI for node type {NodeType}", nodeType);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets available audio generation models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetAudioModelsAsync()
        {
            // Audio models use CheckpointLoaderSimple and should have "ace" or "audio" in their name
            var allModels = await GetAvailableModelsAsync("CheckpointLoaderSimple");
            return allModels
                .Where(m => m.ToLowerInvariant().Contains("ace") || 
                           m.ToLowerInvariant().Contains("audio") ||
                           m.ToLowerInvariant().Contains("step"))
                .ToList();
        }

        /// <summary>
        /// Gets available image generation models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetImageModelsAsync()
        {
            // Image models use CheckpointLoaderSimple and typically include SD, SDXL, or similar
            var allModels = await GetAvailableModelsAsync("CheckpointLoaderSimple");
            return allModels
                .Where(m => !m.ToLowerInvariant().Contains("ace") && 
                           !m.ToLowerInvariant().Contains("svd") &&
                           !m.ToLowerInvariant().Contains("video"))
                .ToList();
        }

        /// <summary>
        /// Gets available video generation models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetVideoModelsAsync()
        {
            // Video models use ImageOnlyCheckpointLoader or contain "svd" in their name
            var imageOnlyModels = await GetAvailableModelsAsync("ImageOnlyCheckpointLoader");
            if (imageOnlyModels.Any())
            {
                return imageOnlyModels;
            }

            // Fallback: check CheckpointLoaderSimple for video models
            var allModels = await GetAvailableModelsAsync("CheckpointLoaderSimple");
            return allModels
                .Where(m => m.ToLowerInvariant().Contains("svd") || 
                           m.ToLowerInvariant().Contains("video"))
                .ToList();
        }

        /// <summary>
        /// Gets available CLIP (text encoder) models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetCLIPModelsAsync()
        {
            return await GetAvailableModelsAsync("CLIPLoader");
        }

        /// <summary>
        /// Gets available VAE models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetVAEModelsAsync()
        {
            return await GetAvailableModelsAsync("VAELoader");
        }

        /// <summary>
        /// Gets available UNET (diffusion model) models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetUNETModelsAsync()
        {
            return await GetAvailableModelsAsync("UNETLoader");
        }

        /// <summary>
        /// Gets available Audio Encoder models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetAudioEncoderModelsAsync()
        {
            return await GetAvailableModelsAsync("AudioEncoderLoader");
        }

        /// <summary>
        /// Gets available LoRA models from ComfyUI
        /// </summary>
        public virtual async Task<List<string>> GetLoRAModelsAsync()
        {
            return await GetAvailableModelsAsync("LoraLoaderModelOnly");
        }



        /// <summary>
        /// Submits a workflow to ComfyUI and returns the prompt ID
        /// </summary>
        public virtual async Task<string?> SubmitWorkflowAsync(object workflowObject)
        {
            try
            {
                _logger.LogInformation("Starting workflow submission process");

                // Convert workflowObject to Dictionary<string, object>
                Dictionary<string, object> promptDict;
                if (workflowObject is Dictionary<string, object> dict)
                {
                    promptDict = dict;
                    _logger.LogDebug("Workflow object is already a Dictionary<string, object>");
                }
                else
                {
                    _logger.LogDebug("Converting workflow object to Dictionary<string, object>");
                    // Try to serialize and deserialize to convert to Dictionary<string, object>
                    var json = JsonSerializer.Serialize(workflowObject);
                    _logger.LogDebug("Serialized workflow to JSON ({Length} characters)", json.Length);
                    promptDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                    _logger.LogDebug("Deserialized JSON to Dictionary with {Count} entries", promptDict.Count);
                }

                if (promptDict.Count == 0)
                {
                    _logger.LogError("Workflow dictionary is empty - cannot submit workflow");
                    return null;
                }

                var request = new PromptRequest
                {
                    Prompt = promptDict,
                    ClientId = Guid.NewGuid().ToString()
                };

                _logger.LogInformation("Submitting workflow to ComfyUI at {ApiUrl}", _settings.ApiUrl);
                _logger.LogDebug("Workflow prompt contains {NodeCount} nodes", promptDict.Count);
                _logger.LogDebug("Request ClientId: {ClientId}", request.ClientId);
                
                _logger.LogDebug("Calling ComfyUI client SubmitPromptAsync...");
                var response = await _comfyUIClient.SubmitPromptAsync(request);
                _logger.LogDebug("Received response from ComfyUI client");

                if (response == null)
                {
                    _logger.LogError("ComfyUI client returned null response");
                    return null;
                }

                _logger.LogDebug("Response PromptId: {PromptId}", response.PromptId ?? "null");
                _logger.LogDebug("Response NodeErrors count: {ErrorCount}", response.NodeErrors?.Count ?? 0);

                if (response.NodeErrors?.Any() == true)
                {
                    _logger.LogError("ComfyUI workflow has node errors: {NodeErrors}", JsonSerializer.Serialize(response.NodeErrors));
                    return null;
                }

                if (string.IsNullOrEmpty(response.PromptId))
                {
                    _logger.LogError("ComfyUI response does not contain a valid PromptId");
                    return null;
                }

                _logger.LogInformation("Workflow submitted successfully with prompt ID: {PromptId}", response.PromptId);
                return response.PromptId;
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, "Workflow validation failed: {ValidationErrors}", ex.Message);
                return null;
            }
            catch (InvalidCastException ex)
            {
                _logger.LogError(ex, "Failed to convert workflow object to Dictionary<string, object>");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting workflow to ComfyUI at {ApiUrl}. Is ComfyUI running and accessible?", _settings.ApiUrl);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout submitting workflow to ComfyUI at {ApiUrl}. The request took longer than expected.", _settings.ApiUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting workflow to ComfyUI at {ApiUrl}", _settings.ApiUrl);
                return null;
            }
        }

        /// <summary>
        /// Polls ComfyUI queue until the specified prompt completes
        /// </summary>
        protected virtual async Task<bool> WaitForCompletionAsync(string promptId, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            var endTime = startTime.Add(timeout);
            var pollInterval = TimeSpan.FromSeconds(_settings.PollIntervalSeconds);
            var pollCount = 0;

            _logger.LogInformation("Starting to poll for completion of prompt {PromptId}. Timeout: {Timeout}, Poll interval: {PollInterval}", 
                promptId, timeout, pollInterval);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    pollCount++;
                    var elapsed = DateTime.UtcNow - startTime;
                    
                    _logger.LogDebug("Poll #{PollCount} for prompt {PromptId} (elapsed: {Elapsed})", 
                        pollCount, promptId, elapsed.ToString(@"hh\:mm\:ss"));

                    var queueResponse = await _comfyUIClient.GetQueueAsync();
                    
                    // Log queue status for debugging
                    var queueCount = queueResponse.QueuePending?.Count ?? 0;
                    var execCount = queueResponse.QueueRunning?.Count ?? 0;
                    _logger.LogDebug("Queue status: {QueueCount} queued, {ExecCount} executing", queueCount, execCount);

                    // Check if our prompt is still in queue or executing
                    var isInQueue = queueResponse.QueuePending?.Any(q => q.PromptId == promptId) == true;
                    var isExecuting = queueResponse.QueueRunning?.Any(q => q.PromptId == promptId) == true;

                    if (isInQueue)
                    {
                        _logger.LogDebug("Prompt {PromptId} is still in queue", promptId);
                    }
                    else if (isExecuting)
                    {
                        _logger.LogDebug("Prompt {PromptId} is currently executing", promptId);
                    }
                    else
                    {
                        // Not in queue or executing, but let's double-check by looking at history
                        // to ensure the task actually completed successfully
                        var historyResponse = await _comfyUIClient.GetHistoryAsync(promptId);
                        
                        if (historyResponse.ContainsKey(promptId))
                        {
                            var promptHistoryElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(historyResponse[promptId]));
                            
                            // Check if there are outputs, indicating successful completion
                            if (promptHistoryElement.TryGetProperty("outputs", out var outputs) && 
                                outputs.EnumerateObject().Any())
                            {
                                _logger.LogInformation("Workflow completed successfully for prompt ID: {PromptId} after {Elapsed} (polls: {PollCount})", 
                                    promptId, elapsed.ToString(@"hh\:mm\:ss"), pollCount);
                                return true;
                            }
                            else
                            {
                                _logger.LogDebug("Prompt {PromptId} finished but outputs not ready yet, waiting for file generation to complete", promptId);
                                // Task finished but outputs not ready yet - wait a bit longer for file generation
                                // This can happen when the task completes but files are still being written
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Prompt {PromptId} not found in history yet, may still be processing", promptId);
                        }
                    }

                    // Log progress every 10 polls (roughly every 100 seconds with 10s intervals)
                    if (pollCount % 10 == 0)
                    {
                        _logger.LogInformation("Still waiting for prompt {PromptId} - elapsed: {Elapsed}, polls: {PollCount}", 
                            promptId, elapsed.ToString(@"hh\:mm\:ss"), pollCount);
                    }

                    await Task.Delay(pollInterval);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling ComfyUI queue status (poll #{PollCount})", pollCount);
                    await Task.Delay(pollInterval);
                }
            }

            var totalElapsed = DateTime.UtcNow - startTime;
            _logger.LogWarning("Workflow timed out for prompt ID: {PromptId} after {TotalElapsed} ({PollCount} polls)", 
                promptId, totalElapsed.ToString(@"hh\:mm\:ss"), pollCount);
            return false;
        }

        /// <summary>
        /// Retrieves the generated files from ComfyUI output directory with retry logic
        /// </summary>
        public virtual async Task<string?> GetGeneratedFileAsync(string promptId, string outputSubfolder, string filePrefix)
        {
            try
            {
                _logger.LogInformation("Retrieving generated file for prompt {PromptId}", promptId);
                
                // Retry logic for history API - sometimes files take a moment to appear in history
                const int maxRetries = 5;
                const int retryDelaySeconds = 3;
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    // Get history for this prompt
                    var historyResponse = await _comfyUIClient.GetHistoryAsync(promptId);
                    if (!historyResponse.ContainsKey(promptId))
                    {
                        _logger.LogWarning("Could not retrieve history for prompt {PromptId}: prompt not found in history", promptId);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                                promptId, attempt + 1, maxRetries);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    var historyJson = JsonSerializer.Serialize(historyResponse[promptId]);
                    _logger.LogDebug("History response for prompt {PromptId} (attempt {Attempt}): {HistoryJson}", 
                        promptId, attempt, historyJson);
                    
                    // Parse history to find output files
                    using var document = JsonDocument.Parse(historyJson);
                    
                    // ComfyUI history structure: { "outputs": { "nodeId": { "images": [...] or "audio": [...] } } }
                    if (!document.RootElement.TryGetProperty("outputs", out var outputs))
                    {
                        _logger.LogWarning("No outputs found for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                            promptId, attempt, maxRetries);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId}, outputs may not be ready yet", promptId);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    // Look for audio or image outputs in any node
                    string? outputFilename = null;
                    string? outputFilenameSubfolder = null;
                    string? fileExtension = null;
                    
                    foreach (var nodeOutput in outputs.EnumerateObject())
                    {
                        // Check for audio outputs
                        if (nodeOutput.Value.TryGetProperty("audio", out var audioArray) && audioArray.GetArrayLength() > 0)
                        {
                            var firstAudio = audioArray[0];
                            if (firstAudio.TryGetProperty("filename", out var filename))
                            {
                                outputFilename = filename.GetString();
                                fileExtension = ".wav";
                            }
                            if (firstAudio.TryGetProperty("subfolder", out var subfolder))
                            {
                                outputFilenameSubfolder = subfolder.GetString();
                            }
                            break;
                        }
                        
                        // Check for image outputs
                        if (nodeOutput.Value.TryGetProperty("images", out var imageArray) && imageArray.GetArrayLength() > 0)
                        {
                            var firstImage = imageArray[0];
                            if (firstImage.TryGetProperty("filename", out var filename))
                            {
                                outputFilename = filename.GetString();
                                // Determine file extension from filename
                                var ext = Path.GetExtension(outputFilename);
                                fileExtension = string.IsNullOrEmpty(ext) ? ".png" : ext;
                            }
                            if (firstImage.TryGetProperty("subfolder", out var subfolder))
                            {
                                outputFilenameSubfolder = subfolder.GetString();
                            }
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(outputFilename))
                    {
                        _logger.LogWarning("No output file found for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                            promptId, attempt, maxRetries);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId}, output files may not be ready yet", promptId);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    // Found output file, proceed with download
                    return await DownloadFileAsync(promptId, outputFilename, outputFilenameSubfolder, outputSubfolder, filePrefix, fileExtension ?? ".png");
                }
                
                // If we reach here, all retries failed
                _logger.LogError("Failed to retrieve generated file for prompt {PromptId} after {MaxRetries} attempts", promptId, maxRetries);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving generated file for prompt {PromptId}", promptId);
                return null;
            }
        }

        /// <summary>
        /// Downloads a file (audio or image) from ComfyUI and saves it locally
        /// </summary>
        private async Task<string?> DownloadFileAsync(string promptId, string filename, string? comfySubfolder, string outputSubfolder, string filePrefix, string fileExtension)
        {
            try
            {
                // Download the file from ComfyUI
                var downloadUrl = string.IsNullOrEmpty(comfySubfolder) 
                    ? $"/view?filename={filename}" 
                    : $"/view?filename={filename}&subfolder={comfySubfolder}";
                    
                _logger.LogInformation("Downloading file: {DownloadUrl} (from subfolder: {ComfySubfolder})", downloadUrl, comfySubfolder ?? "root");
                
                var fileBytes = await _comfyUIClient.GetImageAsync(filename, subfolder: comfySubfolder);
                
                // Create output directory in wwwroot - use the requested outputSubfolder, not the ComfyUI subfolder
                var webRootPath = _environment.WebRootPath;
                var outputsPath = Path.Combine(webRootPath, outputSubfolder);
                
                _logger.LogInformation("Creating output directory - WebRoot: {WebRoot}, OutputSubfolder: {OutputSubfolder}, Full path: {OutputsPath}", 
                    webRootPath, outputSubfolder, outputsPath);
                
                Directory.CreateDirectory(outputsPath);
                _logger.LogDebug("Created/verified output directory: {OutputsPath}", outputsPath);

                // Save the file locally with appropriate extension
                var localFileName = $"{filePrefix}_{promptId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";
                var localFilePath = Path.Combine(outputsPath, localFileName);
                
                _logger.LogInformation("Saving file to: {LocalFilePath}", localFilePath);
                
                await File.WriteAllBytesAsync(localFilePath, fileBytes);
                
                var returnPath = $"/{outputSubfolder}/{localFileName}";
                _logger.LogInformation("File downloaded and saved: {FilePath}, returning web path: {ReturnPath}", localFilePath, returnPath);
                return returnPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file for prompt {PromptId}", promptId);
                return null;
            }
        }

        /// <summary>
        /// Cancels/interrupts a running job in ComfyUI
        /// </summary>
        public virtual async Task<bool> CancelJobAsync(string promptId)
        {
            try
            {
                _logger.LogInformation("Attempting to cancel ComfyUI job with prompt ID: {PromptId}", promptId);
                
                // First try to delete from queue if it's still queued
                var queueRequest = new QueueRequest { Delete = new[] { promptId } };
                await _comfyUIClient.ManageQueueAsync(queueRequest);
                
                _logger.LogInformation("Successfully deleted queued job {PromptId} from ComfyUI queue", promptId);
                
                // Also try to interrupt the execution in case it's running
                var interruptRequest = new InterruptRequest { PromptId = promptId };
                await _comfyUIClient.InterruptAsync(interruptRequest);
                
                _logger.LogInformation("Successfully interrupted ComfyUI execution for prompt {PromptId}", promptId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling ComfyUI job {PromptId}", promptId);
                return false;
            }
        }

        /// <summary>
        /// Abstract method that derived classes must implement for their specific generation logic
        /// </summary>
        public abstract Task<string?> GenerateAsync(VideoSceneOutput sceneOutput);

        /// <summary>
        /// Abstract method for getting the workflow template
        /// </summary>
        public abstract string GetWorkflowTemplate();

        /// <summary>
        /// Checks if a JsonElement contains actual node error information
        /// </summary>
        private static bool HasNodeErrors(JsonElement nodeErrors)
        {
            // If it's null or undefined, no errors
            if (nodeErrors.ValueKind == JsonValueKind.Null || nodeErrors.ValueKind == JsonValueKind.Undefined)
                return false;
            
            // If it's an empty object {}, no errors
            if (nodeErrors.ValueKind == JsonValueKind.Object && !nodeErrors.EnumerateObject().Any())
                return false;
            
            // If it's an empty array [], no errors
            if (nodeErrors.ValueKind == JsonValueKind.Array && !nodeErrors.EnumerateArray().Any())
                return false;
            
            // Otherwise, assume there are errors
            return true;
        }

        /// <summary>
        /// Abstract method for setting the workflow template
        /// </summary>
        public abstract void SetWorkflowTemplate(string template);

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public virtual void Dispose()
        {
            // The ComfyUI client doesn't need to be disposed directly 
            // as it's managed by the DI container and HttpClient is managed by HttpClientFactory
        }
    }
}