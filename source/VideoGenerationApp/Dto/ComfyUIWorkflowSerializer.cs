using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Service for serializing and deserializing ComfyUI workflow JSON
    /// </summary>
    public static class ComfyUIWorkflowSerializer
    {
        private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            // Add custom converters
            options.Converters.Add(new ComfyUIWorkflowLinkConverter());
            options.Converters.Add(new ComfyUIObjectListConverter());
            options.Converters.Add(new ComfyUIDictionaryConverter());

            return options;
        }

        /// <summary>
        /// Deserialize ComfyUI workflow JSON to DTO object
        /// </summary>
        public static ComfyUIWorkflowDto? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ComfyUIWorkflowDto>(json, _serializerOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize ComfyUI workflow JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserialize ComfyUI workflow JSON from file
        /// </summary>
        public static async Task<ComfyUIWorkflowDto?> FromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Workflow file not found: {filePath}");
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load workflow from file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Serialize DTO object to ComfyUI workflow JSON
        /// </summary>
        public static string ToJson(ComfyUIWorkflowDto workflow)
        {
            try
            {
                return JsonSerializer.Serialize(workflow, _serializerOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to serialize ComfyUI workflow to JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Serialize DTO object to ComfyUI workflow JSON file
        /// </summary>
        public static async Task ToFileAsync(ComfyUIWorkflowDto workflow, string filePath)
        {
            try
            {
                var json = ToJson(workflow);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save workflow to file {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clone a workflow by serializing and deserializing
        /// </summary>
        public static ComfyUIWorkflowDto Clone(ComfyUIWorkflowDto workflow)
        {
            var json = ToJson(workflow);
            return FromJson(json) ?? throw new InvalidOperationException("Failed to clone workflow");
        }

        /// <summary>
        /// Validate that a JSON string can be deserialized as a ComfyUI workflow
        /// </summary>
        public static bool IsValidWorkflowJson(string json)
        {
            try
            {
                var workflow = FromJson(json);
                return workflow != null && !string.IsNullOrEmpty(workflow.Id);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get workflow statistics
        /// </summary>
        public static ComfyUIWorkflowStats GetWorkflowStats(ComfyUIWorkflowDto workflow)
        {
            return new ComfyUIWorkflowStats
            {
                NodeCount = workflow.Nodes.Count,
                LinkCount = workflow.Links.Count,
                GroupCount = workflow.Groups.Count,
                NodeTypes = workflow.Nodes.GroupBy(n => n.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastNodeId = workflow.LastNodeId,
                LastLinkId = workflow.LastLinkId,
                Version = workflow.Version
            };
        }
    }

    /// <summary>
    /// Statistics about a ComfyUI workflow
    /// </summary>
    public class ComfyUIWorkflowStats
    {
        public int NodeCount { get; set; }
        public int LinkCount { get; set; }
        public int GroupCount { get; set; }
        public Dictionary<string, int> NodeTypes { get; set; } = new();
        public int LastNodeId { get; set; }
        public int LastLinkId { get; set; }
        public double Version { get; set; }
    }
}