namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a request to the Ollama API for generating text responses from a language model.
    /// </summary>
    public class OllamaPromptRequest
    {
        /// <summary>
        /// The name of the language model to use for generation.
        /// Examples: "llama2", "codellama", "mistral", "phi", "gemma"
        /// Implications: Different models have varying capabilities, response quality, speed, and resource requirements.
        /// Larger models typically produce higher quality but slower responses.
        /// </summary>
        public string model { get; set; } = string.Empty;

        /// <summary>
        /// The input text prompt to send to the model for processing.
        /// Can include system instructions, user queries, or formatted prompts.
        /// Implications: The quality and specificity of the prompt directly affects the response quality.
        /// Well-structured prompts with clear instructions typically yield better results.
        /// </summary>
        public string prompt { get; set; } = string.Empty;

        /// <summary>
        /// Determines whether the response should be streamed in real-time chunks or returned as a complete response.
        /// Values: true (streaming), false (complete response)
        /// Implications: 
        /// - true: Enables real-time response display, better user experience for long responses, but requires handling partial data
        /// - false: Simpler to handle, but user waits for complete response, may seem slower for long generations
        /// </summary>
        public bool stream { get; set; } = false;

        /// <summary>
        /// Specifies the expected output format for the model's response.
        /// Common values: "json", "text", "markdown"
        /// Implications:
        /// - "json": Forces structured JSON output, useful for parsing and data extraction
        /// - "text": Plain text response, most flexible but may require additional parsing
        /// - "markdown": Formatted text with markdown syntax, good for documentation
        /// </summary>
        public string format { get; set; } = "json";

        /// <summary>
        /// Controls how long the model stays loaded in memory after the request completes.
        /// Values: Time duration (e.g., "3m", "10s", "1h") or "0" to unload immediately
        /// Implications:
        /// - Longer durations: Faster subsequent requests, higher memory usage
        /// - Shorter durations: Lower memory usage, slower subsequent requests due to reload time
        /// - "0": Immediately unloads model, saves maximum memory but slowest for follow-up requests
        /// </summary>
        public string keep_alive { get; set; } = "3m";

        /// <summary>
        /// Maximum number of tokens (words/sub-words) the model can generate in its response.
        /// Range: 1 to model's maximum context length (typically 2048-8192+ tokens)
        /// Implications:
        /// - Higher values: Allow longer responses but increase generation time and resource usage
        /// - Lower values: Faster responses but may truncate longer content
        /// - Too low: May cut off important information mid-sentence
        /// </summary>
        public int max_tokens { get; set; } = 8000;

        /// <summary>
        /// Controls the randomness/creativity of the model's responses.
        /// Range: 0.0 (deterministic) to 2.0 (very creative)
        /// Typical range: 0.1 to 1.0
        /// Implications:
        /// - 0.0-0.3: Very focused, deterministic, consistent responses (good for factual content)
        /// - 0.4-0.7: Balanced creativity and consistency (good for general use)
        /// - 0.8-1.0: More creative and varied responses (good for creative writing)
        /// - 1.0+: Highly creative but potentially inconsistent or nonsensical
        /// </summary>
        public float temperature { get; set; } = 0.3f;

        /// <summary>
        /// Controls nucleus sampling - the cumulative probability threshold for token selection.
        /// Range: 0.0 to 1.0
        /// Implications:
        /// - 0.1-0.3: Very focused, considers only the most likely tokens (conservative)
        /// - 0.4-0.7: Balanced selection, good for most applications
        /// - 0.8-0.95: More diverse token selection (creative but coherent)
        /// - 0.95+: Maximum diversity, may include unlikely tokens (can be incoherent)
        /// Works in conjunction with temperature for fine-tuned control.
        /// </summary>
        public float top_p { get; set; } = 0.7f;

        /// <summary>
        /// Number of response variants to generate internally (usually kept at 1 for single responses).
        /// Range: Typically 1.0, can be adjusted for advanced use cases
        /// Implications:
        /// - 1.0: Standard single response generation
        /// - Higher values: May affect response quality and generation time
        /// - This parameter is rarely modified in typical applications
        /// Note: Different from requesting multiple separate responses
        /// </summary>
        public float num_predictions { get; set; } = 1f;
    }
}