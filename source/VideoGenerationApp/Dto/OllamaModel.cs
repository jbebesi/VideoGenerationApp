using System.Text.Json.Serialization;

namespace VideoGenerationApp.Dto
{
    public class OllamaModel
    {
        public string name { get; set; } = string.Empty;
        public long size { get; set; } = 0;
        public string digest { get; set; } = string.Empty;
        public Details? details { get; set; }
        public DateTime modified_at { get; set; }
    }
    
    public class Details
    {
        public string parent_model { get; set; } = string.Empty;
        public string format { get; set; } = string.Empty;
        public string family { get; set; } = string.Empty;
        public List<string>? families { get; set; }
        
        // Use string to handle various formats, then parse as needed
        public string parameter_size { get; set; } = string.Empty;
        public string quantization_level { get; set; } = string.Empty;
        
        // Helper property to get parameter size as number
        [JsonIgnore]
        public long ParameterSizeAsLong
        {
            get
            {
                if (long.TryParse(parameter_size, out long result))
                    return result;
                return 0;
            }
        }
    }
}