using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FourSPM_WebService.Models.Shared
{
    /// <summary>
    /// Generic response class for OData endpoints to ensure proper formatting with '@odata.count' property
    /// </summary>
    /// <typeparam name="T">Type of entities in the response</typeparam>
    public class ODataResponse<T>
    {
        /// <summary>
        /// Collection of entities to be returned in the response
        /// </summary>
        public required IEnumerable<T> Value { get; set; }
        
        /// <summary>
        /// Total count of entities, serialized as '@odata.count' for OData compatibility
        /// </summary>
        [JsonPropertyName("@odata.count")]
        public required int Count { get; set; }

        /// <summary>
        /// Default constructor with empty initializations
        /// </summary>
        public ODataResponse()
        {
            Value = Enumerable.Empty<T>();
            Count = 0;
        }
    }
}
