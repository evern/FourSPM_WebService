using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace FourSPM_WebService.Data.Extensions
{
    public static class ODataExtensions
    {
        /// <summary>
        /// Attempts to parse the related entity's GUID key from an OData navigation link.
        /// This is useful for extracting keys from $ref links when linking navigation properties.
        /// </summary>
        /// <param name="request">The current HTTP request context.</param>
        /// <param name="link">The navigation link URI pointing to the related entity.</param>
        /// <param name="relatedKey">
        /// Output parameter that will hold the parsed GUID key if successful, or null if not.
        /// </param>
        /// <returns>
        /// True if the related key was successfully parsed as a GUID; false otherwise.
        /// </returns>
        public static bool TryParseRelatedKey(this HttpRequest request, Uri link, out Guid? relatedKey)
        {
            // Initialize the output as null
            relatedKey = null;

            // Retrieve the OData EDM model from the current request's route services.
            // This model is required for parsing the OData URI structure.
            var model = request.GetRouteServices().GetService(typeof(IEdmModel)) as IEdmModel;

            // Construct the OData service root URL for this request.
            // This forms the base URI required by the ODataUriParser.
            var serviceRoot = request.CreateODataLink();

            // Initialize the ODataUriParser with:
            // - The EDM model to understand the entity structure
            // - The service root URL as the base URI
            // - The navigation link URI to parse
            var uriParser = new ODataUriParser(model, new Uri(serviceRoot), link);

            // Parse the OData path from the provided link URI.
            // This breaks down the URI into segments (e.g., entity sets, keys, navigation properties).
            // NOTE: ParsePath() can throw exceptions for malformed URIs, so consider adding try-catch for robustness.
            var odataPath = uriParser.ParsePath();

            // Locate the last KeySegment in the parsed OData path.
            // KeySegment represents an entity key, e.g., /Projects(1234) where '1234' is the key.
            var keySegment = odataPath.OfType<KeySegment>().LastOrDefault();

            // If no KeySegment is found, or the key cannot be parsed as a GUID, return false.
            if (keySegment == null || !Guid.TryParse(keySegment.Keys.First().Value.ToString(), out var parsedKey))
            {
                return false;
            }

            // Successfully parsed the related entity's key as a GUID.
            relatedKey = parsedKey;

            // Return true to indicate successful parsing.
            return true;
        }
    }
}
