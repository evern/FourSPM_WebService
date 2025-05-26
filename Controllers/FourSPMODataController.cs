using FourSPM_WebService.Models.Shared;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FourSPM_WebService.Controllers;
public class FourSPMODataController : ODataController
{
    private ApplicationUser? _currentUser;
    private ApplicationUserProvider? _applicationUserProvider;
    
    /// <summary>
    /// Checks if the current user has the specified permission
    /// </summary>
    /// <param name="permissionName">The name of the permission to check for</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    public bool HasPermission(string permissionName)
    {
        // Force CurrentUser to be initialized if it hasn't been already
        
        // Direct match - user has the exact permission
        if (CurrentUser.Permissions.Any(p => p.Name == permissionName))
        {
            return true;
        }
        
        // Handle permission hierarchy - if checking for a view permission, 
        // check if user has the corresponding edit permission
        if (permissionName.EndsWith(".view"))
        {
            // Convert view permission to its edit equivalent
            string editPermission = permissionName.Replace(".view", ".edit");
            
            // Check if user has the edit permission
            return CurrentUser.Permissions.Any(p => p.Name == editPermission);
        }
        
        // No matching permission found
        return false;
    }
    
    /// <summary>
    /// Gets the current ApplicationUser for the request.
    /// This property creates the user directly from HttpContext claims when accessed.
    /// </summary>
    public ApplicationUser CurrentUser
    {
        get
        {
            // Only create the user once per controller instance
            if (_currentUser != null)
            {
                return _currentUser;
            }
            
            // Create a basic user by default (for unauthenticated requests)
            _currentUser = new ApplicationUser();
            
            // Initialize ApplicationUserProvider if needed
            if (HttpContext?.RequestServices != null)
            {
                _applicationUserProvider ??= HttpContext.RequestServices.GetRequiredService<ApplicationUserProvider>();
            }
            
            // Only process authenticated requests
            if (HttpContext?.User?.Identity?.IsAuthenticated == true && _applicationUserProvider != null)
            {
                try
                {
                    // Get user ID from ApplicationUserProvider for proper DB mapping - we know _applicationUserProvider is not null here
                    var userId = _applicationUserProvider!.GetOrCreateUserFromClaimsAsync(HttpContext.User).GetAwaiter().GetResult();
                    
                    // Create ApplicationUser with the mapped user ID
                    if (userId != Guid.Empty && _currentUser != null)
                    {
                        _currentUser.UserId = userId;
                        _currentUser.UserName = HttpContext.User.FindFirst("name")?.Value ??
                                            HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ??
                                            HttpContext.User.FindFirst("preferred_username")?.Value;
                        
                        _currentUser.Email = HttpContext.User.FindFirst("email")?.Value ??
                                         HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                        
                        // Extract the user role from claims
                        _currentUser.Role = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                        
                        // Set permissions based on role
                        var permissions = new List<RolePermissionModel>();
                        
                        if (!string.IsNullOrEmpty(_currentUser.Role))
                        {
                            // Try to determine if the user's role is a system role by checking the database
                            bool isSystemRole = false;
                            try
                            {
                                // Get the database context from the service provider
                                var dbContext = HttpContext.RequestServices.GetService<FourSPMContext>();
                                if (dbContext != null)
                                {
                                    // Look up the role by name from the database to check IS_SYSTEM_ROLE flag
                                    // Use ToLower() on both sides to achieve case insensitivity in a way EF Core can translate
                                    var role = dbContext.ROLEs
                                        .Where(r => r.NAME.ToLower() == (_currentUser.Role != null ? _currentUser.Role.ToLower() : string.Empty) && r.DELETED == null)
                                        .FirstOrDefault();
                                        
                                    if (role != null)
                                    {
                                        isSystemRole = role.IS_SYSTEM_ROLE;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue
                                System.Diagnostics.Debug.WriteLine($"Error checking system role: {ex.Message}");
                            }
                            
                            // Set permissions based on whether the user has a system role
                            if (isSystemRole)
                            {
                                // For system roles, get all static permissions and grant full access to all of them
                                var staticPermissions = FourSPM_WebService.Data.Constants.PermissionConstants.GetStaticPermissions();
                                
                                foreach (var staticPermission in staticPermissions)
                                {
                                    // Add view permission for each feature
                                    permissions.Add(new RolePermissionModel { 
                                        Name = $"{staticPermission.FeatureKey}.view"
                                    });
                                    
                                    // Add edit permission for each feature
                                    permissions.Add(new RolePermissionModel { 
                                        Name = $"{staticPermission.FeatureKey}.edit"
                                    });
                                    
                                    // Add toggle permission if applicable
                                    if (staticPermission.PermissionType == "Toggle")
                                    {
                                        permissions.Add(new RolePermissionModel { 
                                            Name = $"{staticPermission.FeatureKey}.toggle"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                // For non-system roles, fetch the actual permissions from the repository
                                try
                                {
                                    // Get role permissions repository through DI
                                    var rolePermissionRepository = HttpContext.RequestServices.GetService<IRolePermissionRepository>();
                                    // Get the role from the database again to ensure we have a valid reference
                                    var dbContext = HttpContext.RequestServices.GetService<FourSPMContext>();
                                    var roleEntity = dbContext?.ROLEs
                                        .Where(r => r.NAME.ToLower() == (_currentUser.Role != null ? _currentUser.Role.ToLower() : string.Empty) && r.DELETED == null)
                                        .FirstOrDefault();
                                        
                                    if (rolePermissionRepository != null && roleEntity != null)
                                    {
                                        // Get permissions for this role from the repository
                                        var rolePermissions = rolePermissionRepository.GetByRoleIdAsync(roleEntity.GUID).GetAwaiter().GetResult();
                                        
                                        // Only include non-deleted permissions
                                        var activePermissions = rolePermissions.Where(rp => rp.DELETED == null).ToList();
                                        
                                        if (activePermissions.Any())
                                        {
                                            // Map permissions from the database to permission models
                                            foreach (var rp in activePermissions)
                                            {
                                                // Determine permission level based on permission naming convention
                                                // View permissions (.view) get ReadOnly access
                                                // Edit permissions (.edit) and others get All access
                                                var permissionLevel = rp.PERMISSION.EndsWith(".view") ? 
                                                    "ReadOnly" : "All";
                                                    
                                                permissions.Add(new RolePermissionModel
                                                {
                                                    Name = rp.PERMISSION
                                                });
                                            }
                                        }
                                        else
                                        {
                                            // Fallback to basic view permissions if no permissions are defined
                                            permissions.Add(new RolePermissionModel { 
                                                Name = FourSPM_WebService.Data.Constants.PermissionConstants.ProjectsView
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue with minimal permissions
                                    System.Diagnostics.Debug.WriteLine($"Error loading role permissions: {ex.Message}");
                                    
                                    // Fallback to minimal permissions
                                    permissions.Add(new RolePermissionModel { 
                                        Name = FourSPM_WebService.Data.Constants.PermissionConstants.ProjectsView
                                    });
                                }
                            }
                            
                            _currentUser.Permissions = permissions;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - use default user instead
                    System.Diagnostics.Debug.WriteLine($"Error creating ApplicationUser from claims: {ex.Message}");
                }
            }
            
            // We already initialized _currentUser earlier, but add a null check to satisfy the compiler
            return _currentUser ?? new ApplicationUser();
        }
    }
    
    protected IActionResult GetResult<TResult>(OperationResult<TResult> result)
    {
        switch (result.Status)
        {
            case OperationStatus.NoAccess:
                return Unauthorized(result.Message);

            case OperationStatus.NotFound:
                return NotFound(result.Message);

            case OperationStatus.Updated:
                return Updated(result.Result);

            case OperationStatus.Created:
                return Created(result.Result);

            case OperationStatus.Validation:
                return BadRequest(result.Message);

            default:
                return result.Result == null ? NoContent() : Ok(result.Result);
        }
    }

    protected IActionResult GetResult(OperationResult result)
    {
        switch (result.Status)
        {
            case OperationStatus.NoAccess:
                return Unauthorized(result.Message);

            case OperationStatus.NotFound:
                return NotFound(result.Message);

            case OperationStatus.Validation:
                return BadRequest(result.Message);

            default:
                return string.IsNullOrEmpty(result.Message) ? NoContent() : Ok(result.Message);
        }
    }
}