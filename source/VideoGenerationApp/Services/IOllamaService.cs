using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IOllamaService
    {
        Task<List<string>> GetLocalModelsAsync();
        Task<List<OllamaModel>> GetLocalModelsWithDetailsAsync();
        Task<string> SendPromptAsync(OllamaPromptRequest request);
        Task<OllamaResponseWithTiming> SendPromptWithTimingAsync(OllamaPromptRequest request);
        string GetFormattedPrompt(string userPrompt);
        VideoSceneOutput? TryParseVideoSceneOutput(string rawResponse);
    }
}