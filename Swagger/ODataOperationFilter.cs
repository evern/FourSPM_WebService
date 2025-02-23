using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace FourSPM_WebService.Swagger
{
    public class ODataOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionAttributes = context.MethodInfo.GetCustomAttributes(true);
            var enableQueryAttribute = actionAttributes.OfType<EnableQueryAttribute>().FirstOrDefault();

            if (enableQueryAttribute != null)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<OpenApiParameter>();

                // Add OData query parameters
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$select",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Select specific fields",
                    Required = false
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$expand",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Expand related entities",
                    Required = false
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$filter",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Filter the results",
                    Required = false
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$orderby",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "Order the results",
                    Required = false
                });
            }
        }
    }
}
