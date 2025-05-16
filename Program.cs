using FourSPM_WebService.Authorization;
using FourSPM_WebService.Data.Extensions;
using FourSPM_WebService.Middleware;
using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Identity.Web;
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

// Ensure JWT secret is exactly 32 bytes
jwtConfig.Secret = jwtConfig.Secret.PadRight(32).Substring(0, 32);

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
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder
            .WithOrigins(
                "http://localhost:3000",      // Local development
                "https://localhost:3000",     // Local development with HTTPS
                "https://app.4spm.org"        // Production
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
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
        options.RouteOptions.EnableUnqualifiedOperationCall = true;
        options.RouteOptions.EnablePropertyNameCaseInsensitive = true;
        options.RouteOptions.EnableActionNameCaseInsensitive = true;
        options.TimeZone = TimeZoneInfo.Utc;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Add enum string converter to ensure all enums are serialized as strings
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Enable request body buffering
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

builder.Services.AddScoped<IAuthService, AuthService>();

// Add permission-based authorization services
builder.Services.AddPermissionBasedAuthorization();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Add default policy requiring authenticated users
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    // Add policies for basic operations
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Administrator"));
});

// Add Azure AD Authentication with JWT Bearer fallback for legacy support
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    
// Configure token validation parameters for Azure AD
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Set the valid audience to match the Application ID URI
    options.TokenValidationParameters.ValidAudiences = new[]
    {
        $"api://{builder.Configuration["AzureAd:ClientId"]}"
    };
    
    // Only use JWT token validation when Azure AD token validation fails
    options.ForwardDefaultSelector = context =>
    {
        string? authorization = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
        {
            var token = authorization.Substring("Bearer ".Length).Trim();
            // If it looks like a JWT token, use JwtBearerDefaults.AuthenticationScheme
            // Otherwise, use Microsoft.Identity.Web.Constants.Bearer for Azure AD tokens
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return null;
    };
});

// Legacy JWT Authentication - retained for backward compatibility
builder.Services.AddAuthentication()
.AddJwtBearer("LegacyJwt", options =>
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
    app.UseSwaggerUI();
}
else 
{
    // Enable detailed errors even in production temporarily
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");  

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    // Test database connection on first request
    if (context.Request.Path == "/")
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FourSPMContext>();
            await dbContext.Database.CanConnectAsync();
            logger.LogInformation("Database connection test successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection test failed");
            throw; // This will trigger our detailed error page
        }
    }
    
    var headerString = string.Join(", ", context.Request.Headers
        .Select(h => $"{h.Key}={string.Join(",", h.Value.Where(v => v != null))}"));
    logger.LogDebug($"Request headers: {headerString}");

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
