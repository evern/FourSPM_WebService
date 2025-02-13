using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FourSPM_WebService.Swagger.OperationFilter;
public class ODataQueryOptionsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameterDescriptions = context.ApiDescription.ParameterDescriptions;

        foreach (var parameter in parameterDescriptions.ToArray())
        {
            // identify if any of the response types of the end point
            // return a queryable type.
            if (parameter.Type == typeof(ODataQueryOptions) || parameter.Type.IsSubclassOf(typeof(ODataQueryOptions)))
            {
                operation.Parameters.Clear();

                // $filter
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$filter",
                    Description = "Filter the results using OData syntax.",
                    Required = false, // these are all optional filters, so false.
                    In = ParameterLocation.Query, //specify to pass the parameter in the query
                    Schema = new OpenApiSchema { Type = "string" }
                });

                // $orderby
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$orderby",
                    Description = "Order the results using OData syntax",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" }
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$select",
                    Description = "Fields to select using OData syntax",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = "string" }
                });

                // $skip
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$skip",
                    Description = "Skip the specified number of entries",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema
                    {
                        Type = "integer"
                    }
                });

                // $top
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$top",
                    Description = "Get the top x number of records",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema
                    {
                        Type = "integer"
                    }
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "$count",
                    //Description = "Get the top x number of records",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema
                    {
                        Type = "boolean",
                    }
                });
            }
        }
    }
}