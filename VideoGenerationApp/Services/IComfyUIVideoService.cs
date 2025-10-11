using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IComfyUIVideoService
    {
        VideoWorkflowConfig GetWorkflowConfig();
        void SetWorkflowConfig(VideoWorkflowConfig config);
        string GetWorkflowTemplate();
        void SetWorkflowTemplate(string template);
        Task<string?> GenerateAsync(VideoSceneOutput sceneOutput);
        Task<string?> GenerateVideoAsync(VideoWorkflowConfig config);
        Dictionary<string, object> ConvertWorkflowToComfyUIFormat(ComfyUIAudioWorkflow workflow);
        Task<ComfyUIQueueStatus?> GetQueueStatusAsync();
        Task<bool> IsComfyUIRunningAsync();
        Task<List<string>> GetAvailableModelsAsync(string nodeType);
        Task<List<string>> GetVideoModelsAsync();
        Task<bool> CancelJobAsync(string promptId);
        Task<string?> SubmitWorkflowAsync(object workflowObject);
        Task<List<string>> GetUNETModelsAsync();
        Task<List<string>> GetLoRAModelsAsync();
        Task<string?> GetGeneratedFileAsync(string promptId, string outputSubfolder, string filePrefix);
    }
}