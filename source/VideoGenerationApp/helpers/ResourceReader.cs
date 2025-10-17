namespace VideoGenerationApp.helpers
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class ResourceReader
    {
        public static string ReadEmbeddedJson(string resourceName)
        {
            // Default resource from project properties/output (Embedded or Content)
            const string DefaultResource = "VideoGenerationApp.Resources.video_example.json";

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                resourceName = DefaultResource;
            }

            // Search common assemblies for embedded resources
            var assemblies = new Assembly?[]
            {
            Assembly.GetExecutingAssembly(),
            Assembly.GetEntryAssembly(),
            Assembly.GetCallingAssembly()
            }
            .Where(a => a != null)
            .Distinct()
            .ToArray();

            Stream? stream = null;

            // 1) Try exact resource name
            foreach (var asm in assemblies!)
            {
                stream = asm!.GetManifestResourceStream(resourceName);
                if (stream != null) break;
            }

            // 2) Try suffix match to handle differing root namespaces
            if (stream == null)
            {
                foreach (var asm in assemblies!)
                {
                    var match = asm!
                        .GetManifestResourceNames()
                        .FirstOrDefault(n =>
                            n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase) ||
                            n.EndsWith(".video_example.json", StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        stream = asm.GetManifestResourceStream(match);
                        if (stream != null) break;
                    }
                }
            }

            // 3) Fallback: read from file system (Properties output)
            if (stream == null)
            {
                var baseDir = AppContext.BaseDirectory;
                var candidates = new[]
                {
                Path.Combine(baseDir, "Resources", "video_example.json"),
                Path.Combine(baseDir, "video_example.json"),
                Path.Combine(baseDir, "wwwroot", "Resources", "video_example.json")
            };

                var filePath = candidates.FirstOrDefault(File.Exists);
                if (filePath != null)
                {
                    return File.ReadAllText(filePath);
                }
            }

            if (stream == null)
            {
                throw new FileNotFoundException($"Resource '{resourceName}' not found as embedded or file.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}