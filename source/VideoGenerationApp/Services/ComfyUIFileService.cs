using Microsoft.Extensions.Logging;

namespace VideoGenerationApp.Services
{
    /// <summary>
    /// Utility service for ComfyUI file operations (cross-platform)
    /// </summary>
    public interface IComfyUIFileService
    {
        Task<string?> CopyFileToComfyUIInputAsync(string sourceFilePath, string fileType);
        string? GetComfyUIInputDirectory();
    }

    /// <summary>
    /// Cross-platform implementation of ComfyUI file operations
    /// </summary>
    public class ComfyUIFileService : IComfyUIFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ComfyUIFileService> _logger;
        private string? _cachedInputDirectory;

        public ComfyUIFileService(IWebHostEnvironment environment, ILogger<ComfyUIFileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Gets the ComfyUI input directory path (cross-platform)
        /// </summary>
        public string? GetComfyUIInputDirectory()
        {
            if (_cachedInputDirectory != null)
                return _cachedInputDirectory;

            try
            {
                // Try common ComfyUI installation paths (cross-platform)
                var possiblePaths = new List<string>();
                
                // Windows common paths
                if (OperatingSystem.IsWindows())
                {
                    possiblePaths.AddRange(new[]
                    {
                        Path.Combine("C:", "ComfyUI", "input"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ComfyUI", "input"),
                        Path.Combine("D:", "ComfyUI", "input")
                    });
                }
                
                // Linux/macOS common paths
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    possiblePaths.AddRange(new[]
                    {
                        Path.Combine(homeDir, "ComfyUI", "input"),
                        Path.Combine("/opt", "ComfyUI", "input"),
                        Path.Combine("/usr", "local", "ComfyUI", "input"),
                        Path.Combine("/home", "comfyui", "ComfyUI", "input")
                    });
                }
                
                // Common relative paths (cross-platform)
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(".", "ComfyUI", "input"),
                    Path.Combine("..", "ComfyUI", "input"),
                    Path.Combine("..", "..", "ComfyUI", "input")
                });

                // Check environment variable override
                var envPath = Environment.GetEnvironmentVariable("COMFYUI_INPUT_PATH");
                if (!string.IsNullOrEmpty(envPath))
                {
                    possiblePaths.Insert(0, envPath);
                }

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        _cachedInputDirectory = Path.GetFullPath(path); // Normalize the path
                        _logger.LogDebug("Found ComfyUI input directory: {Path}", _cachedInputDirectory);
                        return _cachedInputDirectory;
                    }
                }

                // If no standard path found, try to create one relative to the application
                var webRootPath = _environment.WebRootPath;
                var fallbackPath = Path.Combine(Path.GetDirectoryName(webRootPath) ?? "", "ComfyUI", "input");
                Directory.CreateDirectory(fallbackPath);
                
                _cachedInputDirectory = Path.GetFullPath(fallbackPath);
                _logger.LogInformation("Created ComfyUI input directory: {Path}", _cachedInputDirectory);
                return _cachedInputDirectory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining ComfyUI input directory");
                return null;
            }
        }

        /// <summary>
        /// Copies a file to ComfyUI input directory with proper naming (cross-platform)
        /// </summary>
        public async Task<string?> CopyFileToComfyUIInputAsync(string sourceFilePath, string fileType)
        {
            try
            {
                var comfyUIInputPath = GetComfyUIInputDirectory();
                if (string.IsNullOrEmpty(comfyUIInputPath))
                {
                    _logger.LogError("Could not determine ComfyUI input directory");
                    return null;
                }

                // Convert web path to physical path if needed
                var physicalSourcePath = sourceFilePath;
                if (sourceFilePath.StartsWith("/") || sourceFilePath.StartsWith("\\"))
                {
                    // Handle both Unix-style (/) and Windows-style (\) web paths
                    var relativePath = sourceFilePath.TrimStart('/', '\\');
                    physicalSourcePath = Path.Combine(_environment.WebRootPath, relativePath);
                }

                // Normalize the path to be cross-platform compatible
                physicalSourcePath = Path.GetFullPath(physicalSourcePath);

                if (!File.Exists(physicalSourcePath))
                {
                    _logger.LogError("Source file does not exist: {SourcePath}", physicalSourcePath);
                    return null;
                }

                // Generate a unique filename for ComfyUI (cross-platform safe)
                var sourceFileName = Path.GetFileName(physicalSourcePath);
                var fileExtension = Path.GetExtension(sourceFileName);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                
                // Use only characters that are safe across all filesystems
                var safeGuid = Guid.NewGuid().ToString("N"); // No hyphens
                var uniqueFileName = $"{fileType}_{timestamp}_{safeGuid}{fileExtension}";
                
                var destinationPath = Path.Combine(comfyUIInputPath, uniqueFileName);

                _logger.LogInformation("Copying {FileType} file: {Source} -> {Destination}", fileType, physicalSourcePath, destinationPath);

                // Ensure the destination directory exists
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Copy the file asynchronously
                using var sourceStream = new FileStream(physicalSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await sourceStream.CopyToAsync(destinationStream);

                _logger.LogInformation("Successfully copied {FileType} file to: {Destination}", fileType, destinationPath);

                // Return just the filename (not the full path) for ComfyUI workflows
                return uniqueFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying {FileType} file to ComfyUI input", fileType);
                return null;
            }
        }
    }
}