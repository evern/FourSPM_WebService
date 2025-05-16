using FourSPM_WebService.Services;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Middleware;

public class TokenTypeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenTypeMiddleware> _logger;
    private readonly HashSet<string> _publicEndpoints = new()
    {
        "/api/auth/login",
        "/api/auth/create",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    };

    public TokenTypeMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TokenTypeMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Skip token type detection for public endpoints
        if (path != null && _publicEndpoints.Contains(path))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                // Attempt to parse the token without validation to determine its type
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(token))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    
                    // Determine if this is an MSAL token based on issuer pattern
                    var tenantId = _configuration["AzureAd:TenantId"];
                    var msalIssuers = new[]
                    {
                        $"https://login.microsoftonline.com/{tenantId}/v2.0",
                        $"https://sts.windows.net/{tenantId}/"
                    };
                    
                    bool isMsalToken = msalIssuers.Any(issuer => jwtToken.Issuer?.StartsWith(issuer) == true);
                    
                    // Store token type in HttpContext.Items for other middleware and controllers to access
                    context.Items["IsMsalToken"] = isMsalToken;
                    
                    _logger.LogInformation($"Token type detected: {(isMsalToken ? "MSAL" : "Legacy")} authentication");
                }
            }
            catch (Exception ex)
            {
                // Just log the exception and continue with the pipeline
                // Actual token validation will happen in JwtMiddleware or during auth
                _logger.LogWarning(ex, "Error during token type detection");
            }
        }

        // Continue with the next middleware
        await _next(context);
    }
}
