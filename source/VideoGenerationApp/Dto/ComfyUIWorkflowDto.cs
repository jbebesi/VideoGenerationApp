using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents a complete ComfyUI workflow JSON structure
    /// </summary>
    public class ComfyUIWorkflowDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("revision")]
        public int Revision { get; set; }

        [JsonPropertyName("last_node_id")]
        public int LastNodeId { get; set; }

        [JsonPropertyName("last_link_id")]
        public int LastLinkId { get; set; }

        [JsonPropertyName("nodes")]
        public List<ComfyUIWorkflowNode> Nodes { get; set; } = new();

        [JsonPropertyName("links")]
        public List<ComfyUIWorkflowLink> Links { get; set; } = new();

        [JsonPropertyName("groups")]
        public List<ComfyUIWorkflowGroup> Groups { get; set; } = new();

        [JsonPropertyName("definitions")]
        public ComfyUIWorkflowDefinitions? Definitions { get; set; }

        [JsonPropertyName("config")]
        public Dictionary<string, object> Config { get; set; } = new();

        [JsonPropertyName("extra")]
        public ComfyUIWorkflowExtra? Extra { get; set; }

        [JsonPropertyName("version")]
        public double Version { get; set; }
    }

    /// <summary>
    /// Represents a node in the ComfyUI workflow
    /// </summary>
    public class ComfyUIWorkflowNode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("pos")]
        public List<double> Position { get; set; } = new();

        [JsonPropertyName("size")]
        public List<double> Size { get; set; } = new();

        [JsonPropertyName("flags")]
        public Dictionary<string, object> Flags { get; set; } = new();

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; }

        [JsonPropertyName("inputs")]
        public List<ComfyUIWorkflowNodeInput> Inputs { get; set; } = new();

        [JsonPropertyName("outputs")]
        public List<ComfyUIWorkflowNodeOutput> Outputs { get; set; } = new();

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();

        [JsonPropertyName("widgets_values")]
        public List<object> WidgetValues { get; set; } = new();

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("bgcolor")]
        public string? BackgroundColor { get; set; }
    }

    /// <summary>
    /// Represents an input connection for a node
    /// </summary>
    public class ComfyUIWorkflowNodeInput
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("link")]
        public int? Link { get; set; }

        [JsonPropertyName("widget")]
        public ComfyUIWorkflowWidget? Widget { get; set; }

        [JsonPropertyName("slot_index")]
        public int? SlotIndex { get; set; }
    }

    /// <summary>
    /// Represents an output connection for a node
    /// </summary>
    public class ComfyUIWorkflowNodeOutput
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("links")]
        public List<int> Links { get; set; } = new();

        [JsonPropertyName("shape")]
        public int? Shape { get; set; }

        [JsonPropertyName("slot_index")]
        public int? SlotIndex { get; set; }
    }

    /// <summary>
    /// Represents a widget configuration for a node input
    /// </summary>
    public class ComfyUIWorkflowWidget
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("config")]
        public Dictionary<string, object>? Config { get; set; }
    }

    /// <summary>
    /// Represents a link between nodes in the workflow
    /// </summary>
    public class ComfyUIWorkflowLink
    {
        public int Id { get; set; }
        public int SourceNodeId { get; set; }
        public int SourceOutputIndex { get; set; }
        public int TargetNodeId { get; set; }
        public int TargetInputIndex { get; set; }
        public string DataType { get; set; } = string.Empty;

        // Constructor to handle array format from JSON
        public ComfyUIWorkflowLink() { }

        public ComfyUIWorkflowLink(object[] linkArray)
        {
            if (linkArray.Length >= 6)
            {
                Id = Convert.ToInt32(linkArray[0]);
                SourceNodeId = Convert.ToInt32(linkArray[1]);
                SourceOutputIndex = Convert.ToInt32(linkArray[2]);
                TargetNodeId = Convert.ToInt32(linkArray[3]);
                TargetInputIndex = Convert.ToInt32(linkArray[4]);
                DataType = linkArray[5]?.ToString() ?? string.Empty;
            }
        }

        public object[] ToArray()
        {
            return new object[] { Id, SourceNodeId, SourceOutputIndex, TargetNodeId, TargetInputIndex, DataType };
        }
    }

    /// <summary>
    /// Represents a group in the ComfyUI workflow
    /// </summary>
    public class ComfyUIWorkflowGroup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("bounding")]
        public List<double> Bounding { get; set; } = new();

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;

        [JsonPropertyName("font_size")]
        public int FontSize { get; set; }

        [JsonPropertyName("flags")]
        public Dictionary<string, object> Flags { get; set; } = new();
    }

    /// <summary>
    /// Represents workflow definitions including subgraphs
    /// </summary>
    public class ComfyUIWorkflowDefinitions
    {
        [JsonPropertyName("subgraphs")]
        public List<ComfyUISubgraph> Subgraphs { get; set; } = new();
    }

    /// <summary>
    /// Represents a subgraph definition
    /// </summary>
    public class ComfyUISubgraph
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("nodes")]
        public List<ComfyUIWorkflowNode> Nodes { get; set; } = new();

        [JsonPropertyName("links")]
        public List<ComfyUIWorkflowLink> Links { get; set; } = new();

        [JsonPropertyName("inputs")]
        public List<ComfyUISubgraphPort> Inputs { get; set; } = new();

        [JsonPropertyName("outputs")]
        public List<ComfyUISubgraphPort> Outputs { get; set; } = new();
    }

    /// <summary>
    /// Represents an input or output port for a subgraph
    /// </summary>
    public class ComfyUISubgraphPort
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("node")]
        public int NodeId { get; set; }

        [JsonPropertyName("input")]
        public int? InputIndex { get; set; }

        [JsonPropertyName("output")]
        public int? OutputIndex { get; set; }
    }

    /// <summary>
    /// Represents extra metadata for the workflow
    /// </summary>
    public class ComfyUIWorkflowExtra
    {
        [JsonPropertyName("ds")]
        public Dictionary<string, object> DataStore { get; set; } = new();

        [JsonPropertyName("frontendVersion")]
        public string? FrontendVersion { get; set; }

        [JsonPropertyName("groupNodes")]
        public Dictionary<string, object>? GroupNodes { get; set; }
    }
}