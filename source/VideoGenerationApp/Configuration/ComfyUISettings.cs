namespace VideoGenerationApp.Configuration
{
    /// <summary>
    /// Configuration settings for ComfyUI integration
    /// </summary>
    public class ComfyUISettings
    {
        /// <summary>
        /// The base URL for ComfyUI API
        /// </summary>
        public string ApiUrl { get; set; } = "http://127.0.0.1:8188";

        /// <summary>
        /// Path to the ComfyUI executable or Python executable
        /// If empty, will use "python" and look for ComfyUI in PATH or environment variables
        /// </summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>
        /// Working directory for ComfyUI process
        /// If empty, will use environment variable COMFYUI_PATH or current directory
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Timeout in minutes for ComfyUI operations
        /// </summary>
        public int TimeoutMinutes { get; set; } = 180; // 3 hours for long audio generation

        /// <summary>
        /// Delay in seconds to wait after starting ComfyUI before checking if it's ready
        /// </summary>
        public int StartupDelaySeconds { get; set; } = 3;

        /// <summary>
        /// Interval in seconds between polling for ComfyUI queue status
        /// </summary>
        public int PollIntervalSeconds { get; set; } = 10; // Poll every 10 seconds

        /// <summary>
        /// Gets the full executable command with arguments for starting ComfyUI
        /// </summary>
        public string GetStartupCommand()
        {
            if (!string.IsNullOrEmpty(ExecutablePath) && ExecutablePath.EndsWith(".py"))
            {
                // Direct Python script execution
                return $"python {ExecutablePath} --api";
            }
            else if (!string.IsNullOrEmpty(ExecutablePath))
            {
                // Custom executable
                return $"\"{ExecutablePath}\" --api";
            }
            else if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                // Use main.py from working directory
                return $"python \"{Path.Combine(WorkingDirectory, "main.py")}\" --api";
            }
            else
            {
                // Try to run ComfyUI as a module
                return "python -m comfyui.main --api";
            }
        }

        /// <summary>
        /// Gets the working directory for the ComfyUI process
        /// </summary>
        public string GetWorkingDirectory()
        {
            if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                return WorkingDirectory;
            }

            var envPath = Environment.GetEnvironmentVariable("COMFYUI_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                return envPath;
            }

            return Environment.CurrentDirectory;
        }
    }
}