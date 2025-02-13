using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FourSPM_WebService.Swagger.OperationFilter;

public class ODataDeltaOperationFilter : IOperationFilter
{
    private const string DELTA_WRAPPER = "Microsoft.AspNetCore.OData.Deltas.Delta`1";
    private readonly string? _assemblyName = typeof(Microsoft.AspNetCore.OData.Deltas.Delta).Assembly.FullName;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null) return;

        var deltaTypes =
            operation.RequestBody
                .Content
                .Where(x => x.Value.Schema.Reference?.Id.StartsWith(DELTA_WRAPPER) == true);

        foreach (var (_, value) in deltaTypes)
        {
            var schema = value.Schema;
            var deltaType = Type.GetType(schema.Reference.Id + ", " + _assemblyName);
            var deltaArgument = deltaType?.GetGenericArguments().First();
            schema.Reference.Id = deltaArgument?.Name ?? schema.Reference.Id;
        }
    }
}