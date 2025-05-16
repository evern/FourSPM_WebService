using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
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

        // If the user is already authenticated via Azure AD, skip JWT validation
        if (context.User.Identity?.IsAuthenticated == true && 
            context.User.HasClaim(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier"))
        {
            // User is authenticated via Azure AD
            var appUser = context.RequestServices.GetRequiredService<ApplicationUser>();
            var userIdValue = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            appUser.UserId = !string.IsNullOrEmpty(userIdValue) && Guid.TryParse(userIdValue, out var userId) ? userId : null;
            appUser.UserName = context.User.FindFirst("preferred_username")?.Value ?? string.Empty;
            appUser.Upn = appUser.UserName;
            // Note: You'll need to implement getting permissions based on roles
            // appUser.Permissions = await GetUserPermissionsFromRoles(context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));
            
            await _next(context);
            return;
        }
        
        // Legacy JWT token validation
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            // Get services from the current request scope
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var appUser = context.RequestServices.GetRequiredService<ApplicationUser>();

            if (authService.ValidateToken(token))
            {
                // Get user info from token and populate ApplicationUser
                var userInfo = authService.GetUserInfoFromToken(token);
                if (userInfo != null)
                {
                    appUser.UserId = userInfo.UserId;
                    appUser.UserName = userInfo.UserName;
                    appUser.Upn = userInfo.UserName;
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
