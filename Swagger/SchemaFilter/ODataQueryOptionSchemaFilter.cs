using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FourSPM_WebService.Swagger.SchemaFilter;

public class ODataQueryOptionSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach ((string key, _) in context.SchemaRepository.Schemas)
        {
            if (key.StartsWith(nameof(Microsoft)) || key.StartsWith(nameof(System)))
            {
                context.SchemaRepository.Schemas.Remove(key);
            }
        }
    }
}