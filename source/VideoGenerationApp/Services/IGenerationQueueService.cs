using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    public interface IGenerationQueueService
    {
        event Action<GenerationTask>? TaskStatusChanged;
        
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        
        // New method for the refactored approach
        Task<string> QueueTaskAsync(GenerationTaskBase task);
        
        // Legacy methods for backward compatibility
        Task<string> QueueGenerationAsync(string name, AudioWorkflowConfig config, string? notes = null);
        Task<string> QueueImageGenerationAsync(string name, ImageWorkflowConfig config, string? notes = null);
        Task<string> QueueVideoGenerationAsync(string name, VideoWorkflowConfig config, string? notes = null);
        
        IEnumerable<GenerationTask> GetAllTasks();
        Task<IEnumerable<GenerationTask>> GetAllTasksAsync();
        GenerationTask? GetTask(string taskId);
        Task<bool> CancelTaskAsync(string taskId);
        bool CancelTask(string taskId);
        int ClearCompletedTasks();
        
        Task<List<string>> GetAudioModelsAsync();
        Task<List<string>> GetImageModelsAsync();
        Task<List<string>> GetVideoModelsAsync();
        Task<List<string>> GetCLIPModelsAsync();
        Task<List<string>> GetVAEModelsAsync();
        Task<List<string>> GetUNETModelsAsync();
        Task<List<string>> GetAudioEncoderModelsAsync();
        Task<List<string>> GetLoRAModelsAsync();
    }
}