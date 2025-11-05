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
        private VideoSceneOutput? _videoScene;
        private AudioSection? _audioInputs;
        private ImageSection? _imageInputs;
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
            get { lock(_lock) return _videoScene; }
            set { lock(_lock) _videoScene = value; }
        }

        /// <summary>
        /// Audio generation inputs extracted from the Ollama response
        /// </summary>
        public AudioSection? AudioInputs
        {
            get { lock(_lock) return _audioInputs; }
            set { lock(_lock) _audioInputs = value; }
        }

        /// <summary>
        /// Image generation inputs extracted from the Ollama response
        /// </summary>
        public ImageSection? ImageInputs
        {
            get { lock(_lock) return _imageInputs; }
            set { lock(_lock) _imageInputs = value; }
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
                _videoScene = null; // Will be set separately
                _audioInputs = null;
                _imageInputs = null;
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
                _videoScene = parsedOutput;
                _audioInputs = parsedOutput?.audio;
                _imageInputs = parsedOutput?.image;
                _timestampUtc = DateTimeOffset.UtcNow;
            }
            Changed?.Invoke();
        }

        public void SetParsedOutput(VideoSceneOutput? parsedOutput)
        {
            lock (_lock)
            {
                _videoScene = parsedOutput;
                _audioInputs = parsedOutput?.audio;
                _imageInputs = parsedOutput?.image;
            }
            Changed?.Invoke();
        }

        public void Clear()
        {
            lock (_lock)
            {
                _selectedModel = null;
                _prompt = string.Empty;
                _output = string.Empty;
                _videoScene = null;
                _audioInputs = null;
                _imageInputs = null;
                _timestampUtc = null;
            }
            Changed?.Invoke();
        }
    }
}
