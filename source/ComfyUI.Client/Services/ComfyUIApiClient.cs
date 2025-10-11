using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FluentValidation;
using ComfyUI.Client.Configuration;
using ComfyUI.Client.Models.Requests;
using ComfyUI.Client.Models.Responses;
using ComfyUI.Client.Validators;
using ComfyUI.Client.Extensions;

namespace ComfyUI.Client.Services;

/// <summary>
/// HTTP client for ComfyUI API
/// </summary>
public class ComfyUIApiClient : IComfyUIApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ComfyUIClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    // Validators
    private readonly PromptRequestValidator _promptValidator = new();
    private readonly QueueRequestValidator _queueValidator = new();
    private readonly LogSubscriptionRequestValidator _logSubscriptionValidator = new();
    private readonly FreeMemoryRequestValidator _freeMemoryValidator = new();
    private readonly HistoryRequestValidator _historyValidator = new();

    public ComfyUIApiClient(HttpClient httpClient, IOptions<ComfyUIClientOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = JsonSerializerOptionsExtensions.GetComfyUIDefaults();
    }

    private string GetEndpointUrl(string endpoint)
    {
        var prefix = _options.UseApiPrefix ? "/api" : "";
        return $"{prefix}{endpoint}";
    }

    private async Task ValidateRequestAsync<T>(T request, AbstractValidator<T> validator, CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Request validation failed: {errors}");
        }
    }

    private async Task<T> SendRequestAsync<T>(string endpoint, HttpMethod method, object? content = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, GetEndpointUrl(endpoint));
        
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? errorResponse = null;
            try
            {
                errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
            }
            catch
            {
                // If we can't parse as ErrorResponse, throw with raw content
            }

            var errorMessage = errorResponse?.Error?.Message ?? $"API request failed with status {response.StatusCode}";
            throw new HttpRequestException($"{errorMessage}: {responseContent}");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions) 
                ?? throw new InvalidOperationException("Response content was null");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize response: {ex.Message}. Content: {responseContent}", ex);
        }
    }

    private async Task<byte[]> SendBinaryRequestAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(GetEndpointUrl(endpoint), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task SendRequestAsync(string endpoint, HttpMethod method, object? content = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, GetEndpointUrl(endpoint));
        
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API request failed with status {response.StatusCode}: {responseContent}");
        }
    }

    // Main API endpoints implementation
    public async Task<List<string>> GetEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<List<string>>("/embeddings", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetModelTypesAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<List<string>>("/models", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetModelsAsync(string folder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(folder))
            throw new ArgumentException("Folder cannot be null or empty", nameof(folder));

        return await SendRequestAsync<List<string>>($"/models/{Uri.EscapeDataString(folder)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetExtensionsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<List<string>>("/extensions", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<byte[]> GetImageAsync(string filename, string? type = null, string? subfolder = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));

        var queryParams = new List<string> { $"filename={Uri.EscapeDataString(filename)}" };
        
        if (!string.IsNullOrEmpty(type))
            queryParams.Add($"type={Uri.EscapeDataString(type)}");
        
        if (!string.IsNullOrEmpty(subfolder))
            queryParams.Add($"subfolder={Uri.EscapeDataString(subfolder)}");

        var query = string.Join("&", queryParams);
        return await SendBinaryRequestAsync($"/view?{query}", cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetViewMetadataAsync(string folderName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(folderName))
            throw new ArgumentException("Folder name cannot be null or empty", nameof(folderName));

        return await SendRequestAsync<Dictionary<string, object>>($"/view_metadata/{Uri.EscapeDataString(folderName)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<SystemStatsResponse> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<SystemStatsResponse>("/system_stats", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetFeaturesAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Dictionary<string, object>>("/features", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetPromptInfoAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Dictionary<string, object>>("/prompt", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetObjectInfoAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Dictionary<string, object>>("/object_info", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetObjectInfoAsync(string nodeClass, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nodeClass))
            throw new ArgumentException("Node class cannot be null or empty", nameof(nodeClass));

        return await SendRequestAsync<Dictionary<string, object>>($"/object_info/{Uri.EscapeDataString(nodeClass)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetHistoryAsync(int? maxItems = null, CancellationToken cancellationToken = default)
    {
        var endpoint = "/history";
        if (maxItems.HasValue)
            endpoint += $"?max_items={maxItems}";

        return await SendRequestAsync<Dictionary<string, object>>(endpoint, HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetHistoryAsync(string promptId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty", nameof(promptId));

        return await SendRequestAsync<Dictionary<string, object>>($"/history/{Uri.EscapeDataString(promptId)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<QueueResponse> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<QueueResponse>("/queue", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    // POST endpoints implementation
    public async Task<UploadResponse> UploadFileAsync(byte[] imageData, string filename, string? subfolder = null, string? type = null, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
        
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageData), "image", filename);
        
        if (!string.IsNullOrEmpty(subfolder))
            content.Add(new StringContent(subfolder), "subfolder");
        
        if (!string.IsNullOrEmpty(type))
            content.Add(new StringContent(type), "type");
        
        content.Add(new StringContent(overwrite.ToString().ToLower()), "overwrite");

        using var response = await _httpClient.PostAsync(GetEndpointUrl("/upload/image"), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<UploadResponse>(responseContent, _jsonOptions) 
            ?? throw new InvalidOperationException("Upload response was null");
    }

    public async Task<UploadResponse> UploadMaskAsync(byte[] imageData, string filename, string originalRef, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
        
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        
        if (string.IsNullOrEmpty(originalRef))
            throw new ArgumentException("Original ref cannot be null or empty", nameof(originalRef));

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageData), "image", filename);
        content.Add(new StringContent(originalRef), "original_ref");
        content.Add(new StringContent(overwrite.ToString().ToLower()), "overwrite");

        using var response = await _httpClient.PostAsync(GetEndpointUrl("/upload/mask"), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<UploadResponse>(responseContent, _jsonOptions) 
            ?? throw new InvalidOperationException("Upload response was null");
    }

    public async Task<PromptResponse> SubmitPromptAsync(PromptRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, _promptValidator, cancellationToken);
        return await SendRequestAsync<PromptResponse>("/prompt", HttpMethod.Post, request, cancellationToken);
    }

    public async Task ManageQueueAsync(QueueRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, _queueValidator, cancellationToken);
        await SendRequestAsync("/queue", HttpMethod.Post, request, cancellationToken);
    }

    public async Task InterruptAsync(InterruptRequest? request = null, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("/interrupt", HttpMethod.Post, request ?? new InterruptRequest(), cancellationToken);
    }

    public async Task FreeMemoryAsync(FreeMemoryRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, _freeMemoryValidator, cancellationToken);
        await SendRequestAsync("/free", HttpMethod.Post, request, cancellationToken);
    }

    public async Task ManageHistoryAsync(HistoryRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, _historyValidator, cancellationToken);
        await SendRequestAsync("/history", HttpMethod.Post, request, cancellationToken);
    }

    // Internal API endpoints implementation
    public async Task<string> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<string>("/internal/logs", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<LogsResponse> GetRawLogsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<LogsResponse>("/internal/logs/raw", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task SubscribeToLogsAsync(LogSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, _logSubscriptionValidator, cancellationToken);
        await SendRequestAsync("/internal/logs/subscribe", new HttpMethod("PATCH"), request, cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetFolderPathsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Dictionary<string, string>>("/internal/folder_paths", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetFilesAsync(string directoryType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(directoryType))
            throw new ArgumentException("Directory type cannot be null or empty", nameof(directoryType));
        
        if (!new[] { "output", "input", "temp" }.Contains(directoryType))
            throw new ArgumentException("Directory type must be 'output', 'input', or 'temp'", nameof(directoryType));

        return await SendRequestAsync<List<string>>($"/internal/files/{Uri.EscapeDataString(directoryType)}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}