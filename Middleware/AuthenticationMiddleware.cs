using FourSPM_WebService.Config;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace FourSPM_WebService.Middleware;

/// <summary>
/// Middleware that handles user authentication and permission loading
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly HashSet<string> _publicEndpoints = new()
    {
        "/api/auth/login",
        "/api/auth/create",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/swagger",
        "/health"
    };

    public AuthenticationMiddleware(
        RequestDelegate next, 
        IServiceProvider serviceProvider,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for public endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (_publicEndpoints.Contains(path) || path.StartsWith("/swagger/") || path.StartsWith("/health")))
        {
            _logger.LogDebug("Skipping authentication for public endpoint: {Path}", path);
            await _next(context);
            return;
        }

        try
        {
            // Get the current user from DI
            var appUser = context.RequestServices.GetRequiredService<ApplicationUser>();
            
            // Check if user is authenticated by ASP.NET Core authentication
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Extract claims from the authenticated user
                var nameClaimValue = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var nameIdClaimValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var upnClaimValue = context.User.FindFirst(ClaimConstants.PreferredUserName)?.Value ?? 
                                   context.User.FindFirst("upn")?.Value ?? 
                                   nameClaimValue;

                if (nameClaimValue != null && nameIdClaimValue != null && Guid.TryParse(nameIdClaimValue, out Guid userId))
                {
                    // Populate the application user with basic info
                    appUser.UserName = nameClaimValue;
                    appUser.UserId = userId;
                    appUser.Upn = upnClaimValue;

                    // Load user permissions
                    await LoadUserPermissionsAsync(userId, appUser, context.RequestServices);
                    
                    _logger.LogInformation("User authenticated: {UserName} (ID: {UserId}) with {PermissionCount} permissions", 
                        appUser.UserName, appUser.UserId, appUser.Permissions.Count);
                    
                    await _next(context);
                    return;
                }
                else
                {
                    _logger.LogWarning("Authenticated user missing required claims. Name: {Name}, NameId: {NameId}", 
                        nameClaimValue, nameIdClaimValue);
                }
            }
            else
            {
                // Fallback to check if JWT token is in headers
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    var authService = context.RequestServices.GetRequiredService<IAuthService>();
                    if (authService.ValidateToken(token))
                    {
                        // Get user info from token and populate ApplicationUser
                        var userInfo = authService.GetUserInfoFromToken(token);
                        if (userInfo != null)
                        {
                            appUser.UserId = userInfo.UserId;
                            appUser.UserName = userInfo.UserName;
                            appUser.Upn = userInfo.UserName;
                            
                            // Load user permissions
                            await LoadUserPermissionsAsync(userInfo.UserId, appUser, context.RequestServices);
                            
                            _logger.LogInformation("User authenticated via JWT: {UserName} (ID: {UserId}) with {PermissionCount} permissions", 
                                appUser.UserName, appUser.UserId, appUser.Permissions.Count);
                            
                            await _next(context);
                            return;
                        }
                    }
                }
                
                _logger.LogWarning("Unauthenticated request to protected endpoint: {Path}", path);
            }

            // If we get here, authentication failed
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in authentication middleware");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = "An error occurred during authentication" });
        }
    }

    private async Task LoadUserPermissionsAsync(Guid userId, ApplicationUser appUser, IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FourSPMContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthenticationMiddleware>>();
            
            // Get all permissions from the role permissions table
            var rolePermissions = await dbContext.ROLE_PERMISSIONs
                .Where(rp => !rp.DELETED.HasValue && rp.IS_GRANTED)
                .ToListAsync();
            
            // Group permissions by role name to simulate role-based authorization
            var roleGroups = rolePermissions.GroupBy(rp => rp.ROLE_NAME);
            
            // Create a list to hold all permissions
            var permissions = new List<RolePermissionModel>();
            
            // For now, as a temporary implementation, we'll grant ALL permissions to the authenticated user
            // In a real implementation, you would filter based on the user's actual roles
            foreach (var permission in rolePermissions)
            {
                permissions.Add(new RolePermissionModel
                {
                    Name = permission.PERMISSION_NAME,
                    Permission = Permission.All // Full permissions
                });
            }

            // Log the loaded permissions for debugging
            _logger.LogInformation("Loaded {PermissionCount} permissions from {RoleCount} roles", 
                permissions.Count, roleGroups.Count());

            // Set the permissions on the application user
            appUser.Permissions = permissions;
            
            _logger.LogDebug("Loaded {Count} permissions for user {UserId}", permissions.Count, userId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load permissions for user {UserId}", userId.ToString());
            // Set empty permissions instead of throwing
            appUser.Permissions = new List<RolePermissionModel>();
        }
    }

    // This method will be used later when we have permission types defined
    private Permission GetPermissionFromString(string? permissionType)
    {
        if (string.IsNullOrEmpty(permissionType))
            return Permission.None;
            
        return permissionType.ToLower() switch
        {
            "read" => Permission.ReadOnly,
            "write" => Permission.All,
            "all" => Permission.All,
            _ => Permission.None
        };
    }
}
