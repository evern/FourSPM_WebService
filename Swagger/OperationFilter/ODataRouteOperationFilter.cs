using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Linq;

namespace FourSPM_WebService.Swagger.OperationFilter
{
    public class ODataRouteOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                // Check if this is an OData Get operation with a key parameter
                if (descriptor.ControllerName == "Projects" && descriptor.ActionName == "Get")
                {
                    // Find and modify the key parameter
                    var keyParameter = operation.Parameters.FirstOrDefault(p => p.Name == "key");
                    if (keyParameter != null)
                    {
                        // Update the parameter description
                        keyParameter.Description = "GUID of the project (without quotes or braces)";
                        keyParameter.Example = new Microsoft.OpenApi.Any.OpenApiString("0c43f203-a974-4b2c-868a-16d33b6ed9eb");
                    }

                    // Update the operation URL by removing query parameter
                    var queryKeyParam = operation.Parameters.FirstOrDefault(p => p.Name == "key" && p.In == ParameterLocation.Query);
                    if (queryKeyParam != null)
                    {
                        operation.Parameters.Remove(queryKeyParam);
                    }
                }
            }
        }
    }
}
