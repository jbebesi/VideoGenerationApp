using System.Text.Json;
using System.Text;
using System.Linq;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Configuration;
using Microsoft.Extensions.Options;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Abstract base class for ComfyUI services that provides common functionality
    /// for different ComfyUI use cases (audio, video, image generation, etc.)
    /// </summary>
    public abstract class ComfyUIServiceBase : IDisposable
    {
        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;
        protected readonly IWebHostEnvironment _environment;
        protected readonly ComfyUISettings _settings;

        protected ComfyUIServiceBase(
            HttpClient httpClient, 
            ILogger logger, 
            IWebHostEnvironment environment,
            IOptions<ComfyUISettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _environment = environment;
            _settings = settings.Value;
            
            // Configure HttpClient for ComfyUI
            _httpClient.BaseAddress = new Uri(_settings.ApiUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(_settings.TimeoutMinutes);
        }



        /// <summary>
        /// Checks if ComfyUI is running and responding to API calls
        /// </summary>
        public virtual async Task<bool> IsComfyUIRunningAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/system_stats");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }



        /// <summary>
        /// Submits a workflow to ComfyUI and returns the prompt ID
        /// </summary>
        public virtual async Task<string?> SubmitWorkflowAsync(object workflowObject)
        {
            try
            {
                var request = new ComfyUIWorkflowRequest
                {
                    prompt = workflowObject,
                    client_id = Guid.NewGuid().ToString()
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Submitting workflow to ComfyUI");
                var response = await _httpClient.PostAsync("/prompt", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ComfyUI API error: {StatusCode} - {Content}", response.StatusCode, responseJson);
                    
                    // Try to parse error details if possible
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ComfyUIWorkflowResponse>(responseJson);
                        if (errorResponse?.error != null)
                        {
                            _logger.LogError("ComfyUI workflow error: {ErrorType} - {ErrorMessage}", 
                                errorResponse.error.type, errorResponse.error.message);
                        }
                        if (errorResponse?.node_errors != null && HasNodeErrors(errorResponse.node_errors.Value))
                        {
                            _logger.LogError("ComfyUI node errors: {NodeErrors}", errorResponse.node_errors.Value);
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't parse the error response, just log the raw content
                        _logger.LogError("Failed to parse ComfyUI error response");
                    }
                    
                    return null;
                }

                var workflowResponse = JsonSerializer.Deserialize<ComfyUIWorkflowResponse>(responseJson);

                // Check if there are validation errors even with a 200 response
                if (workflowResponse?.error != null || (workflowResponse?.node_errors != null && HasNodeErrors(workflowResponse.node_errors.Value)))
                {
                    _logger.LogError("ComfyUI workflow validation failed");
                    if (workflowResponse!.error != null)
                    {
                        _logger.LogError("Error: {ErrorType} - {ErrorMessage}", 
                            workflowResponse.error.type, workflowResponse.error.message);
                    }
                    if (workflowResponse.node_errors != null && HasNodeErrors(workflowResponse.node_errors.Value))
                    {
                        _logger.LogError("Node errors: {NodeErrors}", workflowResponse.node_errors.Value);
                    }
                    return null;
                }

                _logger.LogInformation("Workflow submitted successfully with prompt ID: {PromptId}", workflowResponse?.prompt_id);
                return workflowResponse?.prompt_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting workflow to ComfyUI");
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

                    var queueResponse = await _httpClient.GetAsync("/queue");
                    if (!queueResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to get queue status: {StatusCode}", queueResponse.StatusCode);
                        await Task.Delay(pollInterval);
                        continue;
                    }

                    var queueJson = await queueResponse.Content.ReadAsStringAsync();
                    var queueStatus = JsonSerializer.Deserialize<ComfyUIQueueStatus>(queueJson);

                    // Log queue status for debugging
                    var queueCount = queueStatus?.queue?.Count ?? 0;
                    var execCount = queueStatus?.exec?.Count ?? 0;
                    _logger.LogDebug("Queue status: {QueueCount} queued, {ExecCount} executing", queueCount, execCount);

                    // Check if our prompt is still in queue or executing
                    var isInQueue = queueStatus?.queue?.Any(q => q.prompt_id == promptId) == true;
                    var isExecuting = queueStatus?.exec?.Any(q => q.prompt_id == promptId) == true;

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
                        var historyResponse = await _httpClient.GetAsync($"/history/{promptId}");
                        if (historyResponse.IsSuccessStatusCode)
                        {
                            var historyJson = await historyResponse.Content.ReadAsStringAsync();
                            using var historyDoc = JsonDocument.Parse(historyJson);
                            
                            if (historyDoc.RootElement.TryGetProperty(promptId, out var promptHistory))
                            {
                                // Check if there are outputs, indicating successful completion
                                if (promptHistory.TryGetProperty("outputs", out var outputs) && 
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
                        else
                        {
                            _logger.LogDebug("Could not fetch history for prompt {PromptId}, status: {StatusCode}", promptId, historyResponse.StatusCode);
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
                    var historyResponse = await _httpClient.GetAsync($"/history/{promptId}");
                    if (!historyResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Could not retrieve history for prompt {PromptId}: {StatusCode}", 
                            promptId, historyResponse.StatusCode);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                                promptId, attempt + 1, maxRetries);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    var historyJson = await historyResponse.Content.ReadAsStringAsync();
                    _logger.LogDebug("History response for prompt {PromptId} (attempt {Attempt}): {HistoryJson}", 
                        promptId, attempt, historyJson);
                    
                    // Parse history to find output files
                    using var document = JsonDocument.Parse(historyJson);
                    
                    // ComfyUI history structure: { "promptId": { "outputs": { "nodeId": { "images": [...] or "audio": [...] } } } }
                    if (!document.RootElement.TryGetProperty(promptId, out var promptData))
                    {
                        _logger.LogWarning("No history found for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                            promptId, attempt, maxRetries);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId}, history may not be ready yet", promptId);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    if (!promptData.TryGetProperty("outputs", out var outputs))
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

                    // Look for audio outputs in any node
                    string? audioFilename = null;
                    string? audioSubfolder = null;
                    
                    foreach (var nodeOutput in outputs.EnumerateObject())
                    {
                        if (nodeOutput.Value.TryGetProperty("audio", out var audioArray) && audioArray.GetArrayLength() > 0)
                        {
                            var firstAudio = audioArray[0];
                            if (firstAudio.TryGetProperty("filename", out var filename))
                            {
                                audioFilename = filename.GetString();
                            }
                            if (firstAudio.TryGetProperty("subfolder", out var subfolder))
                            {
                                audioSubfolder = subfolder.GetString();
                            }
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(audioFilename))
                    {
                        _logger.LogWarning("No audio output found for prompt {PromptId} (attempt {Attempt}/{MaxRetries})", 
                            promptId, attempt, maxRetries);
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogDebug("Retrying history fetch for prompt {PromptId}, audio files may not be ready yet", promptId);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }
                        return null;
                    }

                    // Found audio file, proceed with download
                    return await DownloadAudioFileAsync(promptId, audioFilename, audioSubfolder, outputSubfolder, filePrefix);
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
        /// Downloads an audio file from ComfyUI and saves it locally
        /// </summary>
        private async Task<string?> DownloadAudioFileAsync(string promptId, string audioFilename, string? audioSubfolder, string outputSubfolder, string filePrefix)
        {
            try
            {
                // Download the file from ComfyUI
                var downloadUrl = string.IsNullOrEmpty(audioSubfolder) 
                    ? $"/view?filename={audioFilename}" 
                    : $"/view?filename={audioFilename}&subfolder={audioSubfolder}";
                    
                _logger.LogInformation("Downloading audio file: {DownloadUrl} (from subfolder: {AudioSubfolder})", downloadUrl, audioSubfolder ?? "root");
                
                var fileResponse = await _httpClient.GetAsync(downloadUrl);
                if (!fileResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to download audio file: {StatusCode}", fileResponse.StatusCode);
                    return null;
                }

                // Create output directory in wwwroot - use the requested outputSubfolder, not the ComfyUI subfolder
                var webRootPath = _environment.WebRootPath;
                var outputsPath = Path.Combine(webRootPath, outputSubfolder);
                
                _logger.LogInformation("Creating output directory - WebRoot: {WebRoot}, OutputSubfolder: {OutputSubfolder}, Full path: {OutputsPath}", 
                    webRootPath, outputSubfolder, outputsPath);
                
                Directory.CreateDirectory(outputsPath);
                _logger.LogDebug("Created/verified output directory: {OutputsPath}", outputsPath);

                // Save the file locally
                var localFileName = $"{filePrefix}_{promptId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.wav";
                var localFilePath = Path.Combine(outputsPath, localFileName);
                
                _logger.LogInformation("Saving file to: {LocalFilePath}", localFilePath);
                
                using var fileStream = File.Create(localFilePath);
                await fileResponse.Content.CopyToAsync(fileStream);
                
                var returnPath = $"/{outputSubfolder}/{localFileName}";
                _logger.LogInformation("Audio file downloaded and saved: {FilePath}, returning web path: {ReturnPath}", localFilePath, returnPath);
                return returnPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading audio file for prompt {PromptId}", promptId);
                return null;
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
            _httpClient?.Dispose();
        }
    }
}