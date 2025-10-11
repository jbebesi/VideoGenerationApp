using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IComfyUIAudioService
    {
        AudioWorkflowConfig GetWorkflowConfig();
        void SetWorkflowConfig(AudioWorkflowConfig config);
        string GetWorkflowTemplate();
        void SetWorkflowTemplate(string template);
        Task<string?> GenerateAsync(VideoSceneOutput sceneOutput);
        Task<ComfyUIQueueStatus?> GetQueueStatusAsync();
        Task<string?> GenerateAudioAsync(VideoSceneOutput sceneOutput);
        Dictionary<string, object> ConvertWorkflowToComfyUIFormat(ComfyUIAudioWorkflow workflow);
        Task<bool> IsComfyUIRunningAsync();
        Task<List<string>> GetAvailableModelsAsync(string nodeType);
        Task<List<string>> GetAudioModelsAsync();
        Task<string?> SubmitWorkflowAsync(object workflowObject);
        Task<bool> CancelJobAsync(string promptId);
        Task<List<string>> GetCLIPModelsAsync();
        Task<List<string>> GetVAEModelsAsync();
        Task<List<string>> GetAudioEncoderModelsAsync();
        Task<string?> GetGeneratedFileAsync(string promptId, string outputSubfolder, string filePrefix);
    }
}