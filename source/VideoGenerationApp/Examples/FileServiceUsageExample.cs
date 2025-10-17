using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;

namespace VideoGenerationApp.Examples
{
    /// <summary>
    /// Example showing how to use the new GeneratedFileService
    /// </summary>
    public class FileServiceUsageExample
    {
        private readonly IGeneratedFileService _fileService;

        public FileServiceUsageExample(IGeneratedFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Example: Load all audio files for a dropdown
        /// </summary>
        public async Task<List<GeneratedFileInfo>> LoadAudioFilesForDropdown()
        {
            var audioFiles = await _fileService.GetAudioFilesAsync();
            
            // Files are already sorted by creation date (newest first)
            // and include useful metadata like duration, size, etc.
            return audioFiles;
        }

        /// <summary>
        /// Example: Check if a specific file exists before using it
        /// </summary>
        public async Task<bool> ValidateSelectedFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return await _fileService.FileExistsAsync(filePath);
        }

        /// <summary>
        /// Example: Get detailed info about a selected file
        /// </summary>
        public async Task<GeneratedFileInfo?> GetFileDetails(string filePath)
        {
            return await _fileService.GetFileInfoAsync(filePath);
        }

        /// <summary>
        /// Example: Load files by type with error handling
        /// </summary>
        public async Task<List<GeneratedFileInfo>> LoadFilesSafely(GenerationType type)
        {
            try
            {
                return await _fileService.GetFilesByTypeAsync(type);
            }
            catch (Exception)
            {
                // Return empty list on error - service already logs errors
                return new List<GeneratedFileInfo>();
            }
        }
    }
}