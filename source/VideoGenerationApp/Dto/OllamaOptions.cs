namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Runtime options that control text generation for Ollama API requests.
    /// These options fine-tune the model's behavior during text generation.
    /// </summary>
    public class OllamaOptions
    {
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
        public float? temperature { get; set; }

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
        public float? top_p { get; set; }

        /// <summary>
        /// Maximum number of tokens (words/sub-words) the model can generate in its response.
        /// Range: 1 to model's maximum context length (typically 2048-8192+ tokens)
        /// Implications:
        /// - Higher values: Allow longer responses but increase generation time and resource usage
        /// - Lower values: Faster responses but may truncate longer content
        /// - Too low: May cut off important information mid-sentence
        /// </summary>
        public int? num_predict { get; set; }

        /// <summary>
        /// Number of response variants to generate internally (usually kept at 1 for single responses).
        /// Range: Typically 1.0, can be adjusted for advanced use cases
        /// Implications:
        /// - 1.0: Standard single response generation
        /// - Higher values: May affect response quality and generation time
        /// - This parameter is rarely modified in typical applications
        /// Note: Different from requesting multiple separate responses
        /// </summary>
        public int? num_ctx { get; set; }

        /// <summary>
        /// Random seed for reproducible generation. Use the same seed for consistent outputs.
        /// </summary>
        public long? seed { get; set; }

        /// <summary>
        /// Stop sequences that will halt generation when encountered.
        /// </summary>
        public string[]? stop { get; set; }

        /// <summary>
        /// Tail free sampling parameter.
        /// </summary>
        public float? tfs_z { get; set; }

        /// <summary>
        /// Locally typical sampling parameter.
        /// </summary>
        public float? typical_p { get; set; }

        /// <summary>
        /// Repetition penalty parameter.
        /// </summary>
        public float? repeat_penalty { get; set; }

        /// <summary>
        /// Repetition penalty window.
        /// </summary>
        public int? repeat_last_n { get; set; }

        /// <summary>
        /// Mirostat sampling mode (1 or 2).
        /// </summary>
        public int? mirostat { get; set; }

        /// <summary>
        /// Mirostat learning rate.
        /// </summary>
        public float? mirostat_tau { get; set; }

        /// <summary>
        /// Mirostat maximum entropy.
        /// </summary>
        public float? mirostat_eta { get; set; }

        /// <summary>
        /// Penalize newlines in generation.
        /// </summary>
        public bool? penalize_newline { get; set; }

        /// <summary>
        /// Number of threads to use for generation.
        /// </summary>
        public int? num_thread { get; set; }

        /// <summary>
        /// Number of GPU layers to use.
        /// </summary>
        public int? num_gpu { get; set; }

        /// <summary>
        /// Low VRAM mode.
        /// </summary>
        public bool? low_vram { get; set; }

        /// <summary>
        /// F16 KV cache.
        /// </summary>
        public bool? f16_kv { get; set; }

        /// <summary>
        /// Vocab only mode.
        /// </summary>
        public bool? vocab_only { get; set; }

        /// <summary>
        /// Use mmap.
        /// </summary>
        public bool? use_mmap { get; set; }

        /// <summary>
        /// Use mlock.
        /// </summary>
        public bool? use_mlock { get; set; }

        /// <summary>
        /// RoPE frequency scaling.
        /// </summary>
        public float? rope_frequency_scale { get; set; }

        /// <summary>
        /// RoPE frequency base.
        /// </summary>
        public float? rope_frequency_base { get; set; }

        /// <summary>
        /// Number of experts to use for MoE models.
        /// </summary>
        public int? num_expert { get; set; }

        /// <summary>
        /// Number of experts to use per token for MoE models.
        /// </summary>
        public int? num_expert_used { get; set; }
    }
}