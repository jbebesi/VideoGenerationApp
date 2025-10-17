using System.Text.Json.Serialization;

namespace ComfyUI.Client.Models.Responses;

/// <summary>
/// Response model for prompt submission
/// </summary>
public class PromptResponse
{
    /// <summary>
    /// The assigned prompt ID
    /// </summary>
    [JsonPropertyName("prompt_id")]
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// The queue number assigned
    /// </summary>
    [JsonPropertyName("number")]
    public double Number { get; set; }

    /// <summary>
    /// Node errors if any
    /// </summary>
    [JsonPropertyName("node_errors")]
    public Dictionary<string, NodeErrorInfo> NodeErrors { get; set; } = new();
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error information
    /// </summary>
    [JsonPropertyName("error")]
    public ErrorInfo Error { get; set; } = new();

    /// <summary>
    /// Node-specific errors
    /// </summary>
    [JsonPropertyName("node_errors")]
    public Dictionary<string, NodeErrorInfo> NodeErrors { get; set; } = new();
}

/// <summary>
/// Error information
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// Error type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error details
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Extra error information
    /// </summary>
    [JsonPropertyName("extra_info")]
    public Dictionary<string, object> ExtraInfo { get; set; } = new();
}

/// <summary>
/// Node error information
/// </summary>
public class NodeErrorInfo
{
    /// <summary>
    /// Class type of the node
    /// </summary>
    [JsonPropertyName("class_type")]
    public string? ClassType { get; set; }

    /// <summary>
    /// List of errors for this node
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ErrorDetail> Errors { get; set; } = new();

    /// <summary>
    /// Dependent outputs affected
    /// </summary>
    [JsonPropertyName("dependent_outputs")]
    public List<string>? DependentOutputs { get; set; }
}

/// <summary>
/// Detailed error information
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error details
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Queue status response
/// </summary>
public class QueueResponse
{
    /// <summary>
    /// Currently running queue items
    /// </summary>
    [JsonPropertyName("queue_running")]
    public List<QueueItem> QueueRunning { get; set; } = new();

    /// <summary>
    /// Pending queue items
    /// </summary>
    [JsonPropertyName("queue_pending")]
    public List<QueueItem> QueuePending { get; set; } = new();
}

/// <summary>
/// Queue item information
/// </summary>
public class QueueItem
{
    /// <summary>
    /// Queue number
    /// </summary>
    public double Number { get; set; }

    /// <summary>
    /// Prompt ID
    /// </summary>
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// Prompt data
    /// </summary>
    public Dictionary<string, object> Prompt { get; set; } = new();

    /// <summary>
    /// Extra data
    /// </summary>
    public Dictionary<string, object> ExtraData { get; set; } = new();

    /// <summary>
    /// Outputs to execute
    /// </summary>
    public List<string> OutputsToExecute { get; set; } = new();
}

/// <summary>
/// System statistics response
/// </summary>
public class SystemStatsResponse
{
    /// <summary>
    /// System information
    /// </summary>
    [JsonPropertyName("system")]
    public SystemInfo System { get; set; } = new();

    /// <summary>
    /// Device information
    /// </summary>
    [JsonPropertyName("devices")]
    public List<DeviceInfo> Devices { get; set; } = new();
}

/// <summary>
/// System information
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Operating system
    /// </summary>
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    /// <summary>
    /// Total RAM in bytes
    /// </summary>
    [JsonPropertyName("ram_total")]
    public long RamTotal { get; set; }

    /// <summary>
    /// Free RAM in bytes
    /// </summary>
    [JsonPropertyName("ram_free")]
    public long RamFree { get; set; }

    /// <summary>
    /// ComfyUI version
    /// </summary>
    [JsonPropertyName("comfyui_version")]
    public string ComfyUIVersion { get; set; } = string.Empty;

    /// <summary>
    /// Required frontend version
    /// </summary>
    [JsonPropertyName("required_frontend_version")]
    public string RequiredFrontendVersion { get; set; } = string.Empty;

    /// <summary>
    /// Python version
    /// </summary>
    [JsonPropertyName("python_version")]
    public string PythonVersion { get; set; } = string.Empty;

    /// <summary>
    /// PyTorch version
    /// </summary>
    [JsonPropertyName("pytorch_version")]
    public string PytorchVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether using embedded Python
    /// </summary>
    [JsonPropertyName("embedded_python")]
    public bool EmbeddedPython { get; set; }

    /// <summary>
    /// Command line arguments
    /// </summary>
    [JsonPropertyName("argv")]
    public List<string> Argv { get; set; } = new();
}

/// <summary>
/// Device information
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Device name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Device type (cuda, cpu, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Device index
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    /// <summary>
    /// Total VRAM in bytes
    /// </summary>
    [JsonPropertyName("vram_total")]
    public long VramTotal { get; set; }

    /// <summary>
    /// Free VRAM in bytes
    /// </summary>
    [JsonPropertyName("vram_free")]
    public long VramFree { get; set; }

    /// <summary>
    /// Total PyTorch VRAM in bytes
    /// </summary>
    [JsonPropertyName("torch_vram_total")]
    public long TorchVramTotal { get; set; }

    /// <summary>
    /// Free PyTorch VRAM in bytes
    /// </summary>
    [JsonPropertyName("torch_vram_free")]
    public long TorchVramFree { get; set; }
}

/// <summary>
/// Upload response
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Uploaded filename
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Subfolder if any
    /// </summary>
    [JsonPropertyName("subfolder")]
    public string? Subfolder { get; set; }

    /// <summary>
    /// Upload type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Log entries response
/// </summary>
public class LogsResponse
{
    /// <summary>
    /// Log entries
    /// </summary>
    [JsonPropertyName("entries")]
    public List<LogEntry> Entries { get; set; } = new();

    /// <summary>
    /// Terminal size information
    /// </summary>
    [JsonPropertyName("size")]
    public TerminalSize Size { get; set; } = new();
}

/// <summary>
/// Log entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Timestamp
    /// </summary>
    [JsonPropertyName("t")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    [JsonPropertyName("m")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Terminal size
/// </summary>
public class TerminalSize
{
    /// <summary>
    /// Number of columns
    /// </summary>
    [JsonPropertyName("cols")]
    public int Cols { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    [JsonPropertyName("rows")]
    public int Rows { get; set; }
}