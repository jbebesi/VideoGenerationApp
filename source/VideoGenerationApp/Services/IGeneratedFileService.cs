using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Interface for loading generated files from wwwroot
    /// </summary>
    public interface IGeneratedFileService
    {
        /// <summary>
        /// Get all available audio files from wwwroot/audio
        /// </summary>
        Task<List<GeneratedFileInfo>> GetAudioFilesAsync();
        
        /// <summary>
        /// Get all available image files from wwwroot/image
        /// </summary>
        Task<List<GeneratedFileInfo>> GetImageFilesAsync();
        
        /// <summary>
        /// Get all available video files from wwwroot/video
        /// </summary>
        Task<List<GeneratedFileInfo>> GetVideoFilesAsync();
        
        /// <summary>
        /// Get all files of a specific type
        /// </summary>
        Task<List<GeneratedFileInfo>> GetFilesByTypeAsync(GenerationType type);
        
        /// <summary>
        /// Check if a file exists
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);
        
        /// <summary>
        /// Get file information for a specific file
        /// </summary>
        Task<GeneratedFileInfo?> GetFileInfoAsync(string filePath);
    }
}