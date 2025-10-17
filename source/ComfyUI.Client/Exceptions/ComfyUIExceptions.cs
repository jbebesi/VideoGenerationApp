namespace ComfyUI.Client.Exceptions;

/// <summary>
/// Exception thrown when ComfyUI API returns an error
/// </summary>
public class ComfyUIApiException : Exception
{
    /// <summary>
    /// Error type returned by the API
    /// </summary>
    public string? ErrorType { get; }

    /// <summary>
    /// Error details returned by the API
    /// </summary>
    public string? ErrorDetails { get; }

    /// <summary>
    /// Node errors if any
    /// </summary>
    public Dictionary<string, object>? NodeErrors { get; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int? StatusCode { get; }

    public ComfyUIApiException(string message) : base(message)
    {
    }

    public ComfyUIApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ComfyUIApiException(string message, string? errorType, string? errorDetails, Dictionary<string, object>? nodeErrors = null, int? statusCode = null) 
        : base(message)
    {
        ErrorType = errorType;
        ErrorDetails = errorDetails;
        NodeErrors = nodeErrors;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when request validation fails
/// </summary>
public class ComfyUIValidationException : Exception
{
    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> ValidationErrors { get; }

    public ComfyUIValidationException(string message, List<string> validationErrors) : base(message)
    {
        ValidationErrors = validationErrors;
    }

    public ComfyUIValidationException(List<string> validationErrors) 
        : base($"Request validation failed: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }
}