using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using FourSPM_WebService.Utilities;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace FourSPM_WebService.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly HashSet<string> _publicEndpoints = new()
    {
        "/api/auth/login",
        "/api/auth/create",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    };

    public JwtMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Skip token validation for public endpoints
        if (path != null && _publicEndpoints.Contains(path))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            // Get services from the current request scope
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var appUser = context.RequestServices.GetRequiredService<ApplicationUser>();

            // Check if the token type has been detected by TokenTypeMiddleware
            bool isMsalToken = context.Items.TryGetValue("IsMsalToken", out var tokenTypeObj) && 
                             tokenTypeObj is bool msalToken && msalToken;
                             
            // For now, we'll use the existing ValidateToken method for both token types
            // In Task #3 and #4, we'll implement separate validation logic
            if (authService.ValidateToken(token))
            {
                // Get user info from token and populate ApplicationUser
                var userInfo = authService.GetUserInfoFromToken(token);
                if (userInfo != null)
                {
                    // Basic identity properties
                    appUser.UserId = userInfo.UserId;
                    appUser.UserName = userInfo.UserName;
                    appUser.Email = userInfo.Email;
                    
                    // Set authentication type
                    appUser.AuthenticationType = userInfo.AuthType;
                    
                    // If this is an MSAL token, populate the MSAL-specific properties
                    if (isMsalToken)
                    {
                        // Use the utility to populate MSAL-specific properties from the token
                        appUser = MsalClaimsUtility.PopulateFromToken(appUser, token);
                        
                        // Ensure ObjectId from userInfo is set as it's critical for user identification
                        if (string.IsNullOrEmpty(appUser.ObjectId) && !string.IsNullOrEmpty(userInfo.ObjectId))
                        {
                            appUser.ObjectId = userInfo.ObjectId;
                        }
                    }
                    else
                    {
                        // For legacy authentication, use username as UPN
                        appUser.Upn = userInfo.UserName;
                    }
                    
                    // Note: You'll need to implement getting permissions
                    // appUser.Permissions = await authService.GetUserPermissions(userInfo.UserId);
                    
                    await _next(context);
                    return;
                }
            }
        }

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Invalid or expired token" });
    }
}
