using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComfyUI.Client.Extensions;

/// <summary>
/// JSON naming policy for snake_case conversion
/// </summary>
public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new List<char>();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Add('_');
            }
            result.Add(char.ToLower(c));
        }

        return new string(result.ToArray());
    }
}

/// <summary>
/// JsonSerializerOptions configured for ComfyUI API
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Get default JsonSerializerOptions for ComfyUI API
    /// </summary>
    /// <returns>Configured JsonSerializerOptions</returns>
    public static JsonSerializerOptions GetComfyUIDefaults()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }
}