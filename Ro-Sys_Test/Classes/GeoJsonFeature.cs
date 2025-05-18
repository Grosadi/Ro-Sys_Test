using System.Text.Json.Serialization;

namespace Ro_Sys_Test.Classes
{
    public class GeoJsonFeature
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Feature";

        [JsonPropertyName("geometry")]
        public GeoJsonGeometry Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
