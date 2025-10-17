using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IOllamaService
    {
        Task<List<string>> GetLocalModelsAsync();
        Task<List<OllamaModel>> GetLocalModelsWithDetailsAsync();
        Task<string> SendPromptAsync(string model, string prompt);
        Task<string> SendPromptAsync(OllamaPromptRequest request);
        string GetFormattedPrompt(string userPrompt);
        VideoSceneOutput? TryParseVideoSceneOutput(string rawResponse);
    }
}