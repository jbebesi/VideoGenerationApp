namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a request to the Ollama API for generating text responses from a language model.
    /// Based on the official Ollama /api/generate endpoint specification.
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
        /// Used for fill-in-the-middle models, text that appears after the user prompt and
        /// before the model response.
        /// </summary>
        public string? suffix { get; set; }

        /// <summary>
        /// Base64-encoded images for models that support image input.
        /// </summary>
        public string[]? images { get; set; }

        /// <summary>
        /// Structured output format for the model to generate a response from.
        /// Supports either the string "json" or a JSON schema object.
        /// </summary>
        public object? format { get; set; }

        /// <summary>
        /// System prompt for the model to generate a response from.
        /// </summary>
        public string? system { get; set; }

        /// <summary>
        /// When true, returns a stream of partial responses.
        /// Default: true
        /// </summary>
        public bool stream { get; set; } = true;

        /// <summary>
        /// When true, returns separate thinking output in addition to content.
        /// </summary>
        public bool? think { get; set; }

        /// <summary>
        /// When true, returns the raw response from the model without any prompt templating.
        /// </summary>
        public bool? raw { get; set; }

        /// <summary>
        /// Model keep-alive duration (for example "5m" or "0" to unload immediately).
        /// </summary>
        public string? keep_alive { get; set; }

        /// <summary>
        /// Runtime options that control text generation.
        /// Contains parameters like temperature, top_p, num_predict, etc.
        /// </summary>
        public OllamaOptions? options { get; set; }
    }
}