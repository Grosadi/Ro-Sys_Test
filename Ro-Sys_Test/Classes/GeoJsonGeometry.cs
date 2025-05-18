using System.Text.Json.Serialization;

namespace Ro_Sys_Test.Classes
{
    public class GeoJsonGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "MultiPolygon";

        [JsonPropertyName("coordinates")]
        public List<List<List<List<double>>>> Coordinates { get; set; } = new();
    }
}
