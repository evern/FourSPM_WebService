using FourSPM_WebService.Data.Extensions;
using FourSPM_WebService.Middleware;
using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.UriParser;
using System.Text;
using FourSPM_WebService.Config;
using FourSPM_WebService.Services;
using Microsoft.IdentityModel.Tokens;
using FourSPM_WebService.Extensions;
using System.Text.Json;
using Azure.Core;

var builder = WebApplication.CreateBuilder(args);

// Add user secrets configuration
builder.Configuration.AddUserSecrets<Program>();

// Debug: Check if secrets are loaded
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("JWT secret not found in configuration!");
}

// Configure JWT settings
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>()
    ?? throw new Exception("JWT configuration is missing in appsettings.json");

// Register configurations
builder.Services.AddSingleton(jwtConfig);

// Add database context
builder.Services.AddDbContext<FourSPMContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddRepositories();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3001"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("OData-Version")
            .AllowCredentials();
    });
});

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.BufferBody = true;
    options.MemoryBufferThreshold = int.MaxValue;
});

// Add Controllers with OData and JSON configuration
builder.Services
    .AddControllers()
    .AddOData(options =>
    {
        options
            .EnableQueryFeatures()
            .SetMaxTop(null)
            .Select()
            .Filter()
            .OrderBy()
            .Count()
            .Expand();

        options.AddRouteComponents(
            "odata/v1",
            EdmModelBuilder.GetEdmModel(),
            svcs =>
            {
                svcs.AddSingleton<ODataBatchHandler, DefaultODataBatchHandler>();
                svcs.AddSingleton(sp => new ODataUriResolver
                {
                    EnableCaseInsensitive = true
                });
            }
        );

        // Standardize on parenthesis format for keys
        options.RouteOptions.EnableKeyAsSegment = false;
        options.RouteOptions.EnableKeyInParenthesis = true;
        options.RouteOptions.EnableQualifiedOperationCall = true;
        options.RouteOptions.EnableUnqualifiedOperationCall = false;
        options.RouteOptions.EnablePropertyNameCaseInsensitive = true;
        options.RouteOptions.EnableActionNameCaseInsensitive = true;
        options.TimeZone = TimeZoneInfo.Utc;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Enable request body buffering
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

builder.Services.AddScoped<IAuthService, AuthService>();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtConfig.Secret))
    };
});

builder.ConfigureInstallers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FourSPM Web Service API v1"));
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Use CORS before auth
app.UseCors("AllowFrontend");

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Incoming {context.Request.Method} request to {context.Request.Path}");
    logger.LogDebug($"Request headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");

    if (context.Request.Method == "PATCH")
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        logger.LogInformation($"PATCH request body: {body}");
        
        // Reset the request body stream position
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var memoryStream = new MemoryStream(bodyBytes);
        context.Request.Body = memoryStream;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseJwtValidation();
app.MapControllers();

app.Run();
