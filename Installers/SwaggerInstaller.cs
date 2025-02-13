using FourSPM_WebService.Interfaces;
using FourSPM_WebService.Swagger.DocumentFilter;
using FourSPM_WebService.Swagger.OperationFilter;
using FourSPM_WebService.Swagger.SchemaFilter;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;

namespace FourSPM_WebService.Installers;

public class SwaggerInstaller : IStartupInstaller
{
    public void Configure(WebApplicationBuilder builder)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => ConfigureSwagger(options, builder.Configuration));
    }

    private static void ConfigureSwagger(SwaggerGenOptions options, IConfiguration configuration)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = $"{nameof(FourSPM_WebService)}"
        });

        //options.ExampleFilters();
        options.CustomSchemaIds(SchemaIdStrategy);
        options.OperationFilter<ODataDeltaOperationFilter>();
        options.OperationFilter<ODataQueryOperationFilter>();
        options.OperationFilter<ODataQueryOptionsOperationFilter>();
        options.DocumentFilter<ODataDocumentFilter>();
        options.SchemaFilter<ODataQueryOptionSchemaFilter>();
    }

    private static string? SchemaIdStrategy(Type currentClass)
    {
        if (!currentClass.Namespace!.StartsWith(nameof(FourSPM_WebService)))
        {
            return currentClass.FullName;
        }

        var displayNameAttr = currentClass
            .GetCustomAttributes(false)
            .OfType<DisplayNameAttribute>()
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(displayNameAttr?.DisplayName))
        {
            return displayNameAttr.DisplayName;
        }

        return GetClassName(currentClass);
    }

    private static string? GetClassName(Type currentClass, string? name = null)
    {
        name = string.IsNullOrEmpty(name) ? currentClass.Name : $"{name}[{currentClass.Name}]";

        foreach (var type in currentClass.GenericTypeArguments)
        {
            name = GetClassName(type, name);
        }

        return name?.Replace("`1", string.Empty);
    }
}