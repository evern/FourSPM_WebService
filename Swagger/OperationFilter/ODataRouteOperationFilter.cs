using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace FourSPM_WebService.Swagger.OperationFilter;

public class ODataRouteOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var odataRouteAttribute = context.MethodInfo.GetCustomAttribute<ODataRouteComponentAttribute>();
        if (odataRouteAttribute != null)
        {
            // Get the route component value from the attribute's constructor parameter
            var routeComponent = odataRouteAttribute.ToString() ?? string.Empty;

            // Update the operation ID to include the OData route
            operation.OperationId = $"{context.MethodInfo.Name}_{routeComponent.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "")}";

            // Ensure parameters are properly documented
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // Extract route parameters from the route component
            var routeParams = routeComponent
                .Split(new[] { '(', ')', '{', '}' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Contains("key"))
                .ToList();

            foreach (var param in routeParams)
            {
                // Add or update route parameters
                var existingParam = operation.Parameters.FirstOrDefault(p => p.Name == param);
                if (existingParam == null)
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = param,
                        In = ParameterLocation.Path,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                        Description = $"The {param} parameter from the OData route"
                    });
                }
            }
        }
    }
}
