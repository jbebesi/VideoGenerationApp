using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IComfyUIImageService
    {
        ImageWorkflowConfig GetWorkflowConfig();
        void SetWorkflowConfig(ImageWorkflowConfig config);
        string GetWorkflowTemplate();
        void SetWorkflowTemplate(string template);
        Task<string?> GenerateAsync(VideoSceneOutput sceneOutput);
        Task<string?> GenerateImageAsync(ImageWorkflowConfig config);
        Dictionary<string, object> ConvertWorkflowToComfyUIFormat(ComfyUIAudioWorkflow workflow);
        Task<ComfyUIQueueStatus?> GetQueueStatusAsync();
        Task<bool> IsComfyUIRunningAsync();
        Task<List<string>> GetAvailableModelsAsync(string nodeType);
        Task<string?> SubmitWorkflowAsync(object workflowObject);
        Task<List<string>> GetImageModelsAsync();
        Task<bool> CancelJobAsync(string promptId);
        Task<string?> GetGeneratedFileAsync(string promptId, string outputSubfolder, string filePrefix);
    }
}