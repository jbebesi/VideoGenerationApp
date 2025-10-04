using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Represents audio-specific instructions and preferences for a video scene
    /// </summary>
    public class AudioSection
    {
        /// <summary>
        /// Type of background music or ambient sounds (e.g., "upbeat instrumental", "nature sounds", "silence")
        /// </summary>
        public string background_music { get; set; } = string.Empty;

        /// <summary>
        /// List of sound effects to accompany actions (e.g., ["applause", "footsteps", "door closing"])
        /// </summary>
        public List<string> sound_effects { get; set; } = new();

        /// <summary>
        /// Overall audio atmosphere (e.g., "energetic", "calm", "mysterious")
        /// </summary>
        public string audio_mood { get; set; } = string.Empty;

        /// <summary>
        /// Relative volume guidance - can be string or object
        /// </summary>
        [JsonConverter(typeof(VolumeConverter))]
        public string volume_levels { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Custom converter to handle volume_levels as either string or object
    /// </summary>
    public class VolumeConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var obj = doc.RootElement;
                var parts = new List<string>();
                
                foreach (var prop in obj.EnumerateObject())
                {
                    string value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.Number => prop.Value.GetDecimal().ToString(),
                        _ => prop.Value.ToString()
                    };
                    parts.Add($"{prop.Name}: {value}");
                }
                
                return string.Join(", ", parts);
            }
            
            return string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}