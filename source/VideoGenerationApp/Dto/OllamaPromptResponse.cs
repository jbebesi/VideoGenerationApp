namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a response from the Ollama API for text generation.
    /// Based on the official Ollama /api/generate endpoint response specification.
    /// </summary>
    public class OllamaPromptResponse
    {
        /// <summary>
        /// The model's generated text response.
        /// </summary>
        public string response { get; set; } = string.Empty;

        /// <summary>
        /// Model name.
        /// </summary>
        public string? model { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of response creation.
        /// </summary>
        public string? created_at { get; set; }

        /// <summary>
        /// The model's generated thinking output (when think=true in request).
        /// </summary>
        public string? thinking { get; set; }

        /// <summary>
        /// Indicates whether generation has finished.
        /// </summary>
        public bool done { get; set; }

        /// <summary>
        /// Reason the generation stopped (e.g., "stop", "length", "function_call").
        /// </summary>
        public string? done_reason { get; set; }

        /// <summary>
        /// Time spent generating the response in nanoseconds.
        /// </summary>
        public long? total_duration { get; set; }

        /// <summary>
        /// Time spent loading the model in nanoseconds.
        /// </summary>
        public long? load_duration { get; set; }

        /// <summary>
        /// Number of input tokens in the prompt.
        /// </summary>
        public int? prompt_eval_count { get; set; }

        /// <summary>
        /// Time spent evaluating the prompt in nanoseconds.
        /// </summary>
        public long? prompt_eval_duration { get; set; }

        /// <summary>
        /// Number of output tokens generated in the response.
        /// </summary>
        public int? eval_count { get; set; }

        /// <summary>
        /// Time spent generating tokens in nanoseconds.
        /// </summary>
        public long? eval_duration { get; set; }

        /// <summary>
        /// Total execution time measured by the client in milliseconds.
        /// </summary>
        public long? execution_time_ms { get; set; }
    }

    /// <summary>
    /// Wrapper class containing both the response text and execution timing information
    /// </summary>
    public class OllamaResponseWithTiming
    {
        /// <summary>
        /// The generated response text
        /// </summary>
        public string ResponseText { get; set; } = string.Empty;

        /// <summary>
        /// Full Ollama response object with timing details
        /// </summary>
        public OllamaPromptResponse FullResponse { get; set; } = new();

        /// <summary>
        /// Client-side execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs => FullResponse.execution_time_ms ?? 0;

        /// <summary>
        /// Server-side total duration in nanoseconds
        /// </summary>
        public long? ServerTotalDurationNs => FullResponse.total_duration;

        /// <summary>
        /// Server-side total duration in milliseconds (converted from nanoseconds)
        /// </summary>
        public double? ServerTotalDurationMs => ServerTotalDurationNs.HasValue
            ? ServerTotalDurationNs.Value / 1_000_000.0
            : null;
    }
}