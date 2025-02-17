using FourSPM_WebService.Data.Extensions;
using FourSPM_WebService.Extensions;
using FourSPM_WebService.Helpers;
using FourSPM_WebService.Services;
using FourSPM_WebService.Config;
using FourSPM_WebService.Controllers;
using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FourSPM_WebService.Middleware;

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

// Add OData services
builder.Services.AddControllers()
    .AddOData(options =>
    {
        options
            .EnableQueryFeatures()
            .SetMaxTop(null)
            .AddRouteComponents("odata/v1", EdmModelBuilder.GetEdmModel(), new DefaultODataBatchHandler());

        options
            .RouteOptions
            .EnableNonParenthesisForEmptyParameterFunction = true;
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevPolicy",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.ConfigureInstallers();
var app = builder.Build();

// Enable CORS
app.UseCors("ReactDevPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseJwtValidation();

app.MapControllers();

app.Run();
