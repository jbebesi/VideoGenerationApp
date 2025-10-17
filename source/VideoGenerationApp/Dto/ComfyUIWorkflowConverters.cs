using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Custom JSON converter for ComfyUI workflow links which are stored as arrays in JSON
    /// </summary>
    public class ComfyUIWorkflowLinkConverter : JsonConverter<ComfyUIWorkflowLink>
    {
        public override ComfyUIWorkflowLink Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected array for ComfyUI workflow link");
            }

            var values = new List<object>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        values.Add(reader.GetInt32());
                        break;
                    case JsonTokenType.String:
                        values.Add(reader.GetString() ?? string.Empty);
                        break;
                    default:
                        values.Add(reader.GetString() ?? string.Empty);
                        break;
                }
            }

            return new ComfyUIWorkflowLink(values.ToArray());
        }

        public override void Write(Utf8JsonWriter writer, ComfyUIWorkflowLink value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Id);
            writer.WriteNumberValue(value.SourceNodeId);
            writer.WriteNumberValue(value.SourceOutputIndex);
            writer.WriteNumberValue(value.TargetNodeId);
            writer.WriteNumberValue(value.TargetInputIndex);
            writer.WriteStringValue(value.DataType);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Custom JSON converter for handling object arrays in widget values and other dynamic content
    /// </summary>
    public class ComfyUIObjectListConverter : JsonConverter<List<object>>
    {
        public override List<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<object>();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                return list;
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out int intValue))
                            list.Add(intValue);
                        else if (reader.TryGetDouble(out double doubleValue))
                            list.Add(doubleValue);
                        break;
                    case JsonTokenType.String:
                        list.Add(reader.GetString() ?? string.Empty);
                        break;
                    case JsonTokenType.True:
                        list.Add(true);
                        break;
                    case JsonTokenType.False:
                        list.Add(false);
                        break;
                    case JsonTokenType.Null:
                        list.Add(null!);
                        break;
                    case JsonTokenType.StartObject:
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                        list.Add(obj ?? new Dictionary<string, object>());
                        break;
                    case JsonTokenType.StartArray:
                        var array = JsonSerializer.Deserialize<List<object>>(ref reader, options);
                        list.Add(array ?? new List<object>());
                        break;
                }
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Custom JSON converter for handling dynamic dictionary content
    /// </summary>
    public class ComfyUIDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<string, object>();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return dictionary;
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string propertyName = reader.GetString() ?? string.Empty;
                reader.Read();

                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out int intValue))
                            dictionary[propertyName] = intValue;
                        else if (reader.TryGetDouble(out double doubleValue))
                            dictionary[propertyName] = doubleValue;
                        break;
                    case JsonTokenType.String:
                        dictionary[propertyName] = reader.GetString() ?? string.Empty;
                        break;
                    case JsonTokenType.True:
                        dictionary[propertyName] = true;
                        break;
                    case JsonTokenType.False:
                        dictionary[propertyName] = false;
                        break;
                    case JsonTokenType.Null:
                        dictionary[propertyName] = null!;
                        break;
                    case JsonTokenType.StartObject:
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                        dictionary[propertyName] = obj ?? new Dictionary<string, object>();
                        break;
                    case JsonTokenType.StartArray:
                        var array = JsonSerializer.Deserialize<List<object>>(ref reader, options);
                        dictionary[propertyName] = array ?? new List<object>();
                        break;
                }
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }
            writer.WriteEndObject();
        }
    }
}