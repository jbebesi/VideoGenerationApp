using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a complete ComfyUI audio generation workflow
    /// </summary>
    public class ComfyUIAudioWorkflow
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public int revision { get; set; } = 0;
        public int last_node_id { get; set; } = 18;
        public int last_link_id { get; set; } = 26;
        public List<ComfyUINode> nodes { get; set; } = new();
        public Dictionary<string, object> extra { get; set; } = new();
        public string version { get; set; } = "0.4";
    }

    /// <summary>
    /// Base class for ComfyUI workflow nodes
    /// </summary>
    public class ComfyUINode
    {
        public int id { get; set; }
        public string type { get; set; } = string.Empty;
        public int[] pos { get; set; } = new int[2];
        public int[] size { get; set; } = new int[2];
        public Dictionary<string, object> flags { get; set; } = new();
        public int order { get; set; }
        public int mode { get; set; }
        public List<ComfyUIInput> inputs { get; set; } = new();
        public List<ComfyUIOutput> outputs { get; set; } = new();
        public Dictionary<string, object> properties { get; set; } = new();
        public object[] widgets_values { get; set; } = Array.Empty<object>();
        public string? color { get; set; }
        public string? bgcolor { get; set; }
    }

    /// <summary>
    /// Input connection for a ComfyUI node
    /// </summary>
    public class ComfyUIInput
    {
        public string localized_name { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public int? link { get; set; }
        public ComfyUIWidget? widget { get; set; }
    }

    /// <summary>
    /// Output connection for a ComfyUI node
    /// </summary>
    public class ComfyUIOutput
    {
        public string localized_name { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public int? slot_index { get; set; }
        public List<int> links { get; set; } = new();
    }

    /// <summary>
    /// Widget configuration for node inputs
    /// </summary>
    public class ComfyUIWidget
    {
        public string name { get; set; } = string.Empty;
    }
}