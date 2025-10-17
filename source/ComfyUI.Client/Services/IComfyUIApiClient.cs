using ComfyUI.Client.Models.Requests;
using ComfyUI.Client.Models.Responses;

namespace ComfyUI.Client.Services;


/// <summary>
/// Interface for ComfyUI API client
/// </summary>
public interface IComfyUIApiClient
{
    // Main API endpoints
    Task<List<string>> GetEmbeddingsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetModelTypesAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetModelsAsync(string folder, CancellationToken cancellationToken = default);
    Task<List<string>> GetExtensionsAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetImageAsync(string filename, string? type = null, string? subfolder = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetViewMetadataAsync(string folderName, CancellationToken cancellationToken = default);
    Task<SystemStatsResponse> GetSystemStatsAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetFeaturesAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetPromptInfoAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetObjectInfoAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetObjectInfoAsync(string nodeClass, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetHistoryAsync(int? maxItems = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetHistoryAsync(string promptId, CancellationToken cancellationToken = default);
    Task<QueueResponse> GetQueueAsync(CancellationToken cancellationToken = default);

    // POST endpoints
    Task<UploadResponse> UploadFileAsync(byte[] imageData, string filename, string? subfolder = null, string? type = null, bool overwrite = false, CancellationToken cancellationToken = default);
    Task<UploadResponse> UploadMaskAsync(byte[] imageData, string filename, string originalRef, bool overwrite = false, CancellationToken cancellationToken = default);
    Task<PromptResponse> SubmitPromptAsync(PromptRequest request, CancellationToken cancellationToken = default);
    Task<PromptResponse> SendWorkflowAsync(string content);
    Task ManageQueueAsync(QueueRequest request, CancellationToken cancellationToken = default);
    Task InterruptAsync(InterruptRequest? request = null, CancellationToken cancellationToken = default);
    Task FreeMemoryAsync(FreeMemoryRequest request, CancellationToken cancellationToken = default);
    Task ManageHistoryAsync(HistoryRequest request, CancellationToken cancellationToken = default);

    // Internal API endpoints
    Task<string> GetLogsAsync(CancellationToken cancellationToken = default);
    Task<LogsResponse> GetRawLogsAsync(CancellationToken cancellationToken = default);
    Task SubscribeToLogsAsync(LogSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetFolderPathsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetFilesAsync(string directoryType, CancellationToken cancellationToken = default);
}