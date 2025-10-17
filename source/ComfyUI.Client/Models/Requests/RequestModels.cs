using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ComfyUI.Client.Models.Requests;

/// <summary>
/// Request model for submitting a workflow prompt
/// </summary>
public class PromptRequest
{
    /// <summary>
    /// The workflow prompt definition
    /// </summary>
    [Required]
    [JsonPropertyName("prompt")]
    public Dictionary<string, object> Prompt { get; set; } = new();

    /// <summary>
    /// Optional prompt ID (will be generated if not provided)
    /// </summary>
    [JsonPropertyName("prompt_id")]
    public string? PromptId { get; set; }

    /// <summary>
    /// Optional number for queue ordering
    /// </summary>
    [JsonPropertyName("number")]
    public double? Number { get; set; }

    /// <summary>
    /// Whether to put the prompt at the front of the queue
    /// </summary>
    [JsonPropertyName("front")]
    public bool? Front { get; set; }

    /// <summary>
    /// Client ID for tracking
    /// </summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Extra data to include with the prompt
    /// </summary>
    [JsonPropertyName("extra_data")]
    public Dictionary<string, object>? ExtraData { get; set; }

    /// <summary>
    /// Specific nodes to execute (partial execution)
    /// </summary>
    [JsonPropertyName("partial_execution_targets")]
    public string[]? PartialExecutionTargets { get; set; }
}

/// <summary>
/// Request model for queue management operations
/// </summary>
public class QueueRequest
{
    /// <summary>
    /// Whether to clear the entire queue
    /// </summary>
    [JsonPropertyName("clear")]
    public bool? Clear { get; set; }

    /// <summary>
    /// List of prompt IDs to delete from queue
    /// </summary>
    [JsonPropertyName("delete")]
    public string[]? Delete { get; set; }
}

/// <summary>
/// Request model for interrupt operations
/// </summary>
public class InterruptRequest
{
    /// <summary>
    /// Specific prompt ID to interrupt (optional - global interrupt if not provided)
    /// </summary>
    [JsonPropertyName("prompt_id")]
    public string? PromptId { get; set; }
}

/// <summary>
/// Request model for free memory operations
/// </summary>
public class FreeMemoryRequest
{
    /// <summary>
    /// Whether to unload models
    /// </summary>
    [JsonPropertyName("unload_models")]
    public bool? UnloadModels { get; set; }

    /// <summary>
    /// Whether to free memory
    /// </summary>
    [JsonPropertyName("free_memory")]
    public bool? FreeMemory { get; set; }
}

/// <summary>
/// Request model for history management operations
/// </summary>
public class HistoryRequest
{
    /// <summary>
    /// Whether to clear the entire history
    /// </summary>
    [JsonPropertyName("clear")]
    public bool? Clear { get; set; }

    /// <summary>
    /// List of prompt IDs to delete from history
    /// </summary>
    [JsonPropertyName("delete")]
    public string[]? Delete { get; set; }
}

/// <summary>
/// Request model for log subscription
/// </summary>
public class LogSubscriptionRequest
{
    /// <summary>
    /// Client ID
    /// </summary>
    [Required]
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable or disable subscription
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}