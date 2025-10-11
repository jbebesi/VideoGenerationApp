using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;

namespace VideoGenerationApp.Examples
{
    /// <summary>
    /// Example showing how to use the new generation workflow pattern
    /// </summary>
    public class GenerationWorkflowExample
    {
        private readonly AudioGenerationWorkflow _audioWorkflow;
        private readonly ImageGenerationWorkflow _imageWorkflow;
        private readonly VideoGenerationWorkflow _videoWorkflow;
        private readonly IGenerationQueueService _queueService;

        public GenerationWorkflowExample(
            AudioGenerationWorkflow audioWorkflow,
            ImageGenerationWorkflow imageWorkflow,
            VideoGenerationWorkflow videoWorkflow,
            IGenerationQueueService queueService)
        {
            _audioWorkflow = audioWorkflow;
            _imageWorkflow = imageWorkflow;
            _videoWorkflow = videoWorkflow;
            _queueService = queueService;
        }

        /// <summary>
        /// Example: Generate audio using the new pattern
        /// </summary>
        public async Task<string> GenerateAudioExample()
        {
            var config = new AudioWorkflowConfig
            {
                Tags = "electronic, ambient",
                Lyrics = "Sample lyrics for audio generation",
                // ... other audio config properties
            };

            // Use the audio workflow service (scoped)
            // It will create the task and queue it with the queue service (singleton)
            var taskId = await _audioWorkflow.GenerateAsync("My Audio Track", config, "Example generation");
            
            return taskId;
        }

        /// <summary>
        /// Example: Generate image using the new pattern
        /// </summary>
        public async Task<string> GenerateImageExample()
        {
            var config = new ImageWorkflowConfig
            {
                PositivePrompt = "A beautiful landscape",
                Width = 1024,
                Height = 1024,
                // ... other image config properties
            };

            // Use the image workflow service (scoped)
            var taskId = await _imageWorkflow.GenerateAsync("My Image", config, "Example generation");
            
            return taskId;
        }

        /// <summary>
        /// Example: Generate video using the new pattern
        /// </summary>
        public async Task<string> GenerateVideoExample()
        {
            var config = new VideoWorkflowConfig
            {
                TextPrompt = "A video of nature",
                ImageFilePath = "/images/input.jpg", // Will be uploaded to ComfyUI
                AudioFilePath = "/audio/background.wav", // Will be uploaded to ComfyUI
                Width = 1024,
                Height = 1024,
                DurationSeconds = 5.0f,
                Fps = 30,
                // ... other video config properties
            };

            // Use the video workflow service (scoped)
            // It will handle file uploads and queue the task
            var taskId = await _videoWorkflow.GenerateAsync("My Video", config, "Example generation");
            
            return taskId;
        }

        /// <summary>
        /// Example: Monitor all tasks
        /// </summary>
        public async Task<IEnumerable<GenerationTask>> GetAllTasksExample()
        {
            // The queue service provides access to all tasks regardless of type
            return await _queueService.GetAllTasksAsync();
        }

        /// <summary>
        /// Example: Cancel a task
        /// </summary>
        public async Task<bool> CancelTaskExample(string taskId)
        {
            // The queue service handles cancellation by delegating to the task itself
            return await _queueService.CancelTaskAsync(taskId);
        }
    }
}