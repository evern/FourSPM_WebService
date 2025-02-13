using System.Reflection;
using FourSPM_WebService.Swagger.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FourSPM_WebService.Swagger.DocumentFilter
{
    public class ODataDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDesc in context.ApiDescriptions)
            {
                if (apiDesc is not { RelativePath: { } })
                {
                    continue;
                }

                if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                {
                    continue;
                }

                if (
                    apiDesc.RelativePath.EndsWith("$count")
                ||
                    apiDesc.RelativePath.Contains("Default.")
                    )
                {
                    swaggerDoc.Paths.Remove($"/{apiDesc.RelativePath}");
                }

                if (methodInfo.GetCustomAttributes(true).Any(attr => attr is SwaggerIgnoreAttribute))
                {
                    swaggerDoc.Paths.Remove($"/{apiDesc.RelativePath}");

                    continue;
                }

                if (methodInfo.DeclaringType == typeof(MetadataController))
                {
                    swaggerDoc.Paths.Remove($"/{apiDesc.RelativePath}");
                }
            }
        }
    }
}
