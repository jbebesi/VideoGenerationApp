namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Extension methods for working with ComfyUI workflow DTOs
    /// </summary>
    public static class ComfyUIWorkflowExtensions
    {
        /// <summary>
        /// Find a node by its ID
        /// </summary>
        public static ComfyUIWorkflowNode? FindNodeById(this ComfyUIWorkflowDto workflow, int nodeId)
        {
            return workflow.Nodes.FirstOrDefault(n => n.Id == nodeId);
        }

        /// <summary>
        /// Find nodes by type
        /// </summary>
        public static IEnumerable<ComfyUIWorkflowNode> FindNodesByType(this ComfyUIWorkflowDto workflow, string nodeType)
        {
            return workflow.Nodes.Where(n => n.Type.Equals(nodeType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find the first node of a specific type
        /// </summary>
        public static ComfyUIWorkflowNode? FindFirstNodeByType(this ComfyUIWorkflowDto workflow, string nodeType)
        {
            return workflow.Nodes.FirstOrDefault(n => n.Type.Equals(nodeType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all unique node types in the workflow
        /// </summary>
        public static IEnumerable<string> GetNodeTypes(this ComfyUIWorkflowDto workflow)
        {
            return workflow.Nodes.Select(n => n.Type).Distinct().OrderBy(t => t);
        }

        /// <summary>
        /// Get links that connect to a specific node
        /// </summary>
        public static IEnumerable<ComfyUIWorkflowLink> GetLinksToNode(this ComfyUIWorkflowDto workflow, int nodeId)
        {
            return workflow.Links.Where(l => l.TargetNodeId == nodeId);
        }

        /// <summary>
        /// Get links that originate from a specific node
        /// </summary>
        public static IEnumerable<ComfyUIWorkflowLink> GetLinksFromNode(this ComfyUIWorkflowDto workflow, int nodeId)
        {
            return workflow.Links.Where(l => l.SourceNodeId == nodeId);
        }

        /// <summary>
        /// Get all nodes that are connected to a specific node (input and output)
        /// </summary>
        public static IEnumerable<ComfyUIWorkflowNode> GetConnectedNodes(this ComfyUIWorkflowDto workflow, int nodeId)
        {
            var connectedNodeIds = workflow.Links
                .Where(l => l.SourceNodeId == nodeId || l.TargetNodeId == nodeId)
                .SelectMany(l => new[] { l.SourceNodeId, l.TargetNodeId })
                .Where(id => id != nodeId)
                .Distinct();

            return workflow.Nodes.Where(n => connectedNodeIds.Contains(n.Id));
        }

        /// <summary>
        /// Update a widget value for a specific node
        /// </summary>
        public static bool UpdateNodeWidgetValue(this ComfyUIWorkflowNode node, int index, object value)
        {
            if (index < 0 || index >= node.WidgetValues.Count)
            {
                return false;
            }

            node.WidgetValues[index] = value;
            return true;
        }

        /// <summary>
        /// Get a widget value from a node
        /// </summary>
        public static T? GetWidgetValue<T>(this ComfyUIWorkflowNode node, int index)
        {
            if (index < 0 || index >= node.WidgetValues.Count)
            {
                return default;
            }

            var value = node.WidgetValues[index];
            if (value is T directValue)
            {
                return directValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Add a new node to the workflow
        /// </summary>
        public static ComfyUIWorkflowNode AddNode(this ComfyUIWorkflowDto workflow, string nodeType, double x = 0, double y = 0)
        {
            var node = new ComfyUIWorkflowNode
            {
                Id = ++workflow.LastNodeId,
                Type = nodeType,
                Position = new List<double> { x, y },
                Size = new List<double> { 200, 100 },
                Order = workflow.Nodes.Count,
                Mode = 0
            };

            workflow.Nodes.Add(node);
            return node;
        }

        /// <summary>
        /// Add a link between two nodes
        /// </summary>
        public static ComfyUIWorkflowLink AddLink(this ComfyUIWorkflowDto workflow, 
            int sourceNodeId, int sourceOutputIndex, 
            int targetNodeId, int targetInputIndex, 
            string dataType = "")
        {
            var link = new ComfyUIWorkflowLink
            {
                Id = ++workflow.LastLinkId,
                SourceNodeId = sourceNodeId,
                SourceOutputIndex = sourceOutputIndex,
                TargetNodeId = targetNodeId,
                TargetInputIndex = targetInputIndex,
                DataType = dataType
            };

            workflow.Links.Add(link);
            return link;
        }

        /// <summary>
        /// Remove a node and all its connections
        /// </summary>
        public static bool RemoveNode(this ComfyUIWorkflowDto workflow, int nodeId)
        {
            var node = workflow.FindNodeById(nodeId);
            if (node == null)
            {
                return false;
            }

            // Remove all links connected to this node
            workflow.Links.RemoveAll(l => l.SourceNodeId == nodeId || l.TargetNodeId == nodeId);

            // Remove the node
            workflow.Nodes.Remove(node);
            return true;
        }

        /// <summary>
        /// Remove a specific link
        /// </summary>
        public static bool RemoveLink(this ComfyUIWorkflowDto workflow, int linkId)
        {
            var link = workflow.Links.FirstOrDefault(l => l.Id == linkId);
            if (link == null)
            {
                return false;
            }

            workflow.Links.Remove(link);
            return true;
        }

        /// <summary>
        /// Validate the workflow structure
        /// </summary>
        public static ComfyUIWorkflowValidationResult Validate(this ComfyUIWorkflowDto workflow)
        {
            var result = new ComfyUIWorkflowValidationResult { IsValid = true };

            // Check for duplicate node IDs
            var duplicateNodeIds = workflow.Nodes
                .GroupBy(n => n.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNodeIds.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Duplicate node IDs found: {string.Join(", ", duplicateNodeIds)}");
            }

            // Check for invalid links
            foreach (var link in workflow.Links)
            {
                var sourceNode = workflow.FindNodeById(link.SourceNodeId);
                var targetNode = workflow.FindNodeById(link.TargetNodeId);

                if (sourceNode == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Link {link.Id} references non-existent source node {link.SourceNodeId}");
                }

                if (targetNode == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Link {link.Id} references non-existent target node {link.TargetNodeId}");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Result of workflow validation
    /// </summary>
    public class ComfyUIWorkflowValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}