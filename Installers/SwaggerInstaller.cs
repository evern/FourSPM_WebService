using FourSPM_WebService.Interfaces;
using FourSPM_WebService.Swagger;
using FourSPM_WebService.Swagger.DocumentFilter;
using FourSPM_WebService.Swagger.OperationFilter;
using FourSPM_WebService.Swagger.SchemaFilter;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using Microsoft.AspNetCore.OData.Deltas;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Installers;

public class SwaggerInstaller : IStartupInstaller
{
    public void Configure(WebApplicationBuilder builder)
    {
        builder.Services.ConfigureSwagger();
    }
}

public static class SwaggerInstallerExtensions
{
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "FourSPM Web Service API",
                Description = "FourSPM Web Service API with OData support"
            });

            // Configure JWT authentication for Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add Delta<ProjectEntity> schema mapping
            options.MapType<Delta<ProjectEntity>>(() => new OpenApiSchema
            {
                Type = "object",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = "string" // Allows arbitrary properties
                }
            });

            // Enable XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // Enable OData operations in Swagger - use only necessary filters
            options.DocumentFilter<ODataDocumentFilter>();
            options.SchemaFilter<ODataQueryOptionSchemaFilter>();
            options.OperationFilter<ODataDeltaOperationFilter>();
            options.OperationFilter<ODataRouteOperationFilter>();

            // Use only one filter for OData query parameters
            options.OperationFilter<ODataQueryOperationFilter>();

            options.CustomSchemaIds(SchemaIdStrategy);
        });

        return services;
    }

    private static string? SchemaIdStrategy(Type currentClass)
    {
        var str = currentClass.Name;
        if (currentClass.IsGenericType)
        {
            var name = currentClass.GetGenericArguments()
                .Select(SchemaIdStrategy)
                .Aggregate(
                    currentClass.Name.Split('`').First(),
                    (current, arg) => current + "_" + arg);
            str = name;
        }
        return str;
    }
}