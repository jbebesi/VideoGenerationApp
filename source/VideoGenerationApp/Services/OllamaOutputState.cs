using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    // Per-circuit state for sharing Ollama outputs across pages
    public class OllamaOutputState
    {
        public string? SelectedModel { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public VideoSceneOutput? ParsedOutput { get; set; }
        public DateTimeOffset? TimestampUtc { get; set; }

        public event Action? Changed;

        public void Set(string? selectedModel, string prompt, string output)
        {
            SelectedModel = selectedModel;
            Prompt = prompt;
            Output = output;
            ParsedOutput = null; // Will be set separately
            TimestampUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
        }

        public void Set(string? selectedModel, string prompt, string output, VideoSceneOutput? parsedOutput)
        {
            SelectedModel = selectedModel;
            Prompt = prompt;
            Output = output;
            ParsedOutput = parsedOutput;
            TimestampUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
        }

        public void SetParsedOutput(VideoSceneOutput? parsedOutput)
        {
            ParsedOutput = parsedOutput;
            Changed?.Invoke();
        }

        public void Clear()
        {
            SelectedModel = null;
            Prompt = string.Empty;
            Output = string.Empty;
            ParsedOutput = null;
            TimestampUtc = null;
            Changed?.Invoke();
        }
    }
}
