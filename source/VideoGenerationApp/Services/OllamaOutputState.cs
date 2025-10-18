using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    // Singleton state for sharing Ollama outputs across pages and navigation
    public class OllamaOutputState
    {
        private readonly object _lock = new object();
        
        private string? _selectedModel;
        private string _prompt = string.Empty;
        private string _output = string.Empty;
        private VideoSceneOutput? _parsedOutput;
        private MultiFieldOutput? _multiFieldOutput;
        private DateTimeOffset? _timestampUtc;

        public string? SelectedModel 
        { 
            get { lock(_lock) return _selectedModel; }
            set { lock(_lock) _selectedModel = value; }
        }
        
        public string Prompt 
        { 
            get { lock(_lock) return _prompt; }
            set { lock(_lock) _prompt = value; }
        }
        
        public string Output 
        { 
            get { lock(_lock) return _output; }
            set { lock(_lock) _output = value; }
        }
        
        public VideoSceneOutput? ParsedOutput 
        { 
            get { lock(_lock) return _parsedOutput; }
            set { lock(_lock) _parsedOutput = value; }
        }
        
        public MultiFieldOutput? MultiFieldOutput 
        { 
            get { lock(_lock) return _multiFieldOutput; }
            set { lock(_lock) _multiFieldOutput = value; }
        }
        
        public DateTimeOffset? TimestampUtc 
        { 
            get { lock(_lock) return _timestampUtc; }
            set { lock(_lock) _timestampUtc = value; }
        }

        public event Action? Changed;

        public void Set(string? selectedModel, string prompt, string output)
        {
            lock (_lock)
            {
                _selectedModel = selectedModel;
                _prompt = prompt;
                _output = output;
                _parsedOutput = null; // Will be set separately
                _timestampUtc = DateTimeOffset.UtcNow;
            }
            Changed?.Invoke();
        }

        public void Set(string? selectedModel, string prompt, string output, VideoSceneOutput? parsedOutput)
        {
            lock (_lock)
            {
                _selectedModel = selectedModel;
                _prompt = prompt;
                _output = output;
                _parsedOutput = parsedOutput;
                _timestampUtc = DateTimeOffset.UtcNow;
            }
            Changed?.Invoke();
        }

        public void SetParsedOutput(VideoSceneOutput? parsedOutput)
        {
            lock (_lock)
            {
                _parsedOutput = parsedOutput;
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// Sets multi-field output generated from multiple prompts
        /// </summary>
        public void SetMultiField(string? selectedModel, MultiFieldOutput multiFieldOutput)
        {
            lock (_lock)
            {
                _selectedModel = selectedModel;
                _multiFieldOutput = multiFieldOutput;
                
                // Maintain backward compatibility
                if (multiFieldOutput.LegacyOutput != null)
                {
                    _parsedOutput = multiFieldOutput.LegacyOutput;
                }
                
                // Set combined output for legacy prompt/output fields
                var contents = multiFieldOutput.Fields.Values
                    .Select(f => $"{f.FieldType}: {f.GeneratedContent}")
                    .ToList();
                
                _prompt = "Multi-field generation";
                _output = string.Join("\n\n", contents);
                
                _timestampUtc = DateTimeOffset.UtcNow;
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// Gets content for a specific field type from multi-field output
        /// </summary>
        public string GetFieldContent(ContentFieldType fieldType)
        {
            lock (_lock)
            {
                return _multiFieldOutput?.GetContent(fieldType) ?? string.Empty;
            }
        }

        /// <summary>
        /// Checks if content exists for a specific field type
        /// </summary>
        public bool HasFieldContent(ContentFieldType fieldType)
        {
            lock (_lock)
            {
                return _multiFieldOutput?.HasContent(fieldType) ?? false;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _selectedModel = null;
                _prompt = string.Empty;
                _output = string.Empty;
                _parsedOutput = null;
                _multiFieldOutput = null;
                _timestampUtc = null;
            }
            Changed?.Invoke();
        }
    }
}
