using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents different types of content fields that can be generated
    /// </summary>
    public enum ContentFieldType
    {
        PositivePrompt,
        NegativePrompt,
        Lyrics,
        Tags
    }

    /// <summary>
    /// Configuration for generating content for a specific field type
    /// </summary>
    public class ContentFieldConfig
    {
        public ContentFieldType FieldType { get; set; }
        public int MinLength { get; set; } = 10;
        public int MaxLength { get; set; } = 500;
        public bool IsEnabled { get; set; } = true;
        public string Context { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generated content for a specific field type
    /// </summary>
    public class ContentFieldResult
    {
        public ContentFieldType FieldType { get; set; }
        public string GeneratedContent { get; set; } = string.Empty;
        public int ActualLength { get; set; }
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Multi-field output containing generated content for multiple field types
    /// </summary>
    public class MultiFieldOutput
    {
        public Dictionary<ContentFieldType, ContentFieldResult> Fields { get; set; } = new();
        
        /// <summary>
        /// Legacy VideoSceneOutput for backward compatibility
        /// </summary>
        public VideoSceneOutput? LegacyOutput { get; set; }

        /// <summary>
        /// Gets generated content for a specific field type
        /// </summary>
        public string GetContent(ContentFieldType fieldType)
        {
            return Fields.TryGetValue(fieldType, out var result) ? result.GeneratedContent : string.Empty;
        }

        /// <summary>
        /// Sets generated content for a specific field type
        /// </summary>
        public void SetContent(ContentFieldType fieldType, string content)
        {
            Fields[fieldType] = new ContentFieldResult
            {
                FieldType = fieldType,
                GeneratedContent = content,
                ActualLength = content.Length,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Checks if content exists for a specific field type
        /// </summary>
        public bool HasContent(ContentFieldType fieldType)
        {
            return Fields.ContainsKey(fieldType) && !string.IsNullOrWhiteSpace(Fields[fieldType].GeneratedContent);
        }

        /// <summary>
        /// Gets all available field types with content
        /// </summary>
        public IEnumerable<ContentFieldType> AvailableFields => Fields.Keys;
    }
}