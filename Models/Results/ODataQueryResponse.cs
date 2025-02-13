using System.Text.Json.Serialization;

namespace FourSPM_WebService.Models.Results
{
    public class ODataQueryResponse<TEntity>
    {
        [JsonPropertyName("@odata.context")]
        public required string Context { get; set; }

        [JsonPropertyName("@odata.count")]
        public int? Count { get; set; }

        [JsonPropertyName("value")]
        public required IEnumerable<TEntity> Value { get; set; }
    }
}
