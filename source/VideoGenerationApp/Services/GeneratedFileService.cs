using VideoGenerationApp.Dto;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Service for loading generated files from wwwroot folders
    /// </summary>
    public class GeneratedFileService : IGeneratedFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GeneratedFileService> _logger;
        
        private readonly Dictionary<GenerationType, string> _folderMap = new()
        {
            { GenerationType.Audio, "audio" },
            { GenerationType.Image, "image" },
            { GenerationType.Video, "video" }
        };
        
        private readonly Dictionary<GenerationType, string[]> _extensionMap = new()
        {
            { GenerationType.Audio, new[] { ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".aac" } },
            { GenerationType.Image, new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" } },
            { GenerationType.Video, new[] { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".wmv" } }
        };

        public GeneratedFileService(IWebHostEnvironment environment, ILogger<GeneratedFileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<GeneratedFileInfo>> GetAudioFilesAsync()
        {
            return await GetFilesByTypeAsync(GenerationType.Audio);
        }

        public async Task<List<GeneratedFileInfo>> GetImageFilesAsync()
        {
            return await GetFilesByTypeAsync(GenerationType.Image);
        }

        public async Task<List<GeneratedFileInfo>> GetVideoFilesAsync()
        {
            return await GetFilesByTypeAsync(GenerationType.Video);
        }

        public async Task<List<GeneratedFileInfo>> GetFilesByTypeAsync(GenerationType type)
        {
            try
            {
                if (!_folderMap.TryGetValue(type, out var folderName))
                {
                    _logger.LogWarning("Unknown generation type: {Type}", type);
                    return new List<GeneratedFileInfo>();
                }

                var folderPath = Path.Combine(_environment.WebRootPath, folderName);
                
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogDebug("Folder does not exist: {FolderPath}", folderPath);
                    return new List<GeneratedFileInfo>();
                }

                var validExtensions = _extensionMap[type];
                var files = Directory.GetFiles(folderPath)
                    .Where(file => validExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => CreateFileInfo(file, type))
                    .Where(info => info != null)
                    .Cast<GeneratedFileInfo>()
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                _logger.LogDebug("Found {Count} {Type} files in {FolderPath}", files.Count, type, folderPath);
                return await Task.FromResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading {Type} files", type);
                return new List<GeneratedFileInfo>();
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // Handle both absolute and relative paths
                string fullPath;
                if (Path.IsPathRooted(filePath))
                {
                    fullPath = filePath;
                }
                else
                {
                    // Remove leading slash if present and combine with wwwroot
                    var normalizedPath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    fullPath = Path.Combine(_environment.WebRootPath, normalizedPath);
                }

                return await Task.FromResult(File.Exists(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<GeneratedFileInfo?> GetFileInfoAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !await FileExistsAsync(filePath))
                    return null;

                // Convert relative path to absolute
                string fullPath;
                if (Path.IsPathRooted(filePath))
                {
                    fullPath = filePath;
                }
                else
                {
                    var normalizedPath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    fullPath = Path.Combine(_environment.WebRootPath, normalizedPath);
                }

                // Determine type based on folder
                var relativePath = Path.GetRelativePath(_environment.WebRootPath, fullPath);
                var folderName = relativePath.Split(Path.DirectorySeparatorChar)[0].ToLowerInvariant();
                
                var type = folderName switch
                {
                    "audio" => GenerationType.Audio,
                    "image" => GenerationType.Image,
                    "video" => GenerationType.Video,
                    _ => GenerationType.Audio // Default fallback
                };

                return CreateFileInfo(fullPath, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for: {FilePath}", filePath);
                return null;
            }
        }

        private GeneratedFileInfo? CreateFileInfo(string fullPath, GenerationType type)
        {
            try
            {
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                    return null;

                // Extract name from filename (remove prefix and timestamp if present)
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var displayName = ExtractDisplayName(fileName, type);

                var result = new GeneratedFileInfo
                {
                    Name = displayName,
                    FilePath = fullPath,
                    Type = type,
                    Size = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTime,
                    Extension = fileInfo.Extension.ToLowerInvariant()
                };

                // Try to extract additional metadata based on filename patterns
                ExtractMetadataFromFilename(fileName, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file info for: {FullPath}", fullPath);
                return null;
            }
        }

        private static string ExtractDisplayName(string fileName, GenerationType type)
        {
            // Remove common prefixes and timestamp patterns
            var name = fileName;
            
            // Remove type prefix (e.g., "audio_", "image_", "video_")
            var prefix = type.ToString().ToLowerInvariant() + "_";
            if (name.StartsWith(prefix))
                name = name.Substring(prefix.Length);

            // Remove timestamp patterns (e.g., "_20231201_143022")
            var timestampPattern = @"_\d{8}_\d{6}$";
            name = System.Text.RegularExpressions.Regex.Replace(name, timestampPattern, "");

            // Remove prompt ID patterns (e.g., "_prompt-xyz")
            var promptPattern = @"_prompt-[a-zA-Z0-9]+$";
            name = System.Text.RegularExpressions.Regex.Replace(name, promptPattern, "");

            return string.IsNullOrEmpty(name) ? fileName : name;
        }

        private static void ExtractMetadataFromFilename(string fileName, GeneratedFileInfo fileInfo)
        {
            // Try to extract metadata from filename patterns
            // This is a basic implementation - you might want to use actual media libraries for accurate metadata

            try
            {
                // Look for resolution patterns like "1024x768" or "1920x1080"
                var resolutionMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{3,4})x(\d{3,4})");
                if (resolutionMatch.Success)
                {
                    if (int.TryParse(resolutionMatch.Groups[1].Value, out var width) &&
                        int.TryParse(resolutionMatch.Groups[2].Value, out var height))
                    {
                        fileInfo.Width = width;
                        fileInfo.Height = height;
                    }
                }

                // Look for duration patterns like "30s" or "2m30s"
                var durationMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d+(?:\.\d+)?)s");
                if (durationMatch.Success)
                {
                    if (float.TryParse(durationMatch.Groups[1].Value, out var seconds))
                    {
                        fileInfo.Duration = seconds;
                    }
                }
            }
            catch
            {
                // Ignore metadata extraction errors
            }
        }
    }
}