namespace ComfyUI.Client.Configuration;

/// <summary>
/// Configuration options for ComfyUI client
/// </summary>
public class ComfyUIClientOptions
{
    /// <summary>
    /// Base URL for the ComfyUI API
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8188";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes default for long-running operations

    /// <summary>
    /// Whether to use /api prefix for endpoints
    /// </summary>
    public bool UseApiPrefix { get; set; } = true;
}