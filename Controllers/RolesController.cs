using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.Mapping;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;
using FourSPM_WebService.Attributes;
using FourSPM_WebService.Data.Constants;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class RolesController : FourSPMODataController
    {
        private readonly IRoleRepository _repository;
        private readonly IRolePermissionRepository _permissionRepository;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            IRoleRepository repository,
            IRolePermissionRepository permissionRepository,
            ILogger<RolesController> logger)
        {
            _repository = repository;
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.RolesView)]
        public async Task<IActionResult> Get()
        {
            try
            {
                var roles = await _repository.GetAllAsync();
                var entities = roles.Select(r => RoleMapper.ToEntity(r));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, "Internal server error");
            }
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.RolesView)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var role = await _repository.GetByIdAsync(key);
                if (role == null)
                    return NotFound();

                return Ok(RoleMapper.ToEntity(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }

        [RequirePermission(PermissionConstants.RolesEdit)]
        public async Task<IActionResult> Post([FromBody] RoleEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var role = new ROLE
                {
                    GUID = entity.Guid,
                    NAME = entity.Name,
                    DISPLAY_NAME = entity.DisplayName,
                    DESCRIPTION = entity.Description,
                    IS_SYSTEM_ROLE = entity.IsSystemRole,
                    CREATED = DateTime.UtcNow,
                    CREATEDBY = CurrentUser.UserId ?? Guid.Empty
                };

                var result = await _repository.CreateAsync(role);
                return Created(RoleMapper.ToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, "Internal server error");
            }
        }

        [RequirePermission(PermissionConstants.RolesEdit)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] RoleEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (key != entity.Guid)
                    return BadRequest("The ID in the URL must match the ID in the request body");

                var role = new ROLE
                {
                    GUID = entity.Guid,
                    NAME = entity.Name,
                    DISPLAY_NAME = entity.DisplayName,
                    DESCRIPTION = entity.Description,
                    IS_SYSTEM_ROLE = entity.IsSystemRole,
                    CREATED = DateTime.UtcNow,
                    CREATEDBY = CurrentUser.UserId ?? Guid.Empty
                };

                var result = await _repository.UpdateAsync(role);
                if (result == null)
                    return NotFound($"Role with ID {key} not found");
                    
                return Updated(RoleMapper.ToEntity(result));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }

        [RequirePermission(PermissionConstants.RolesEdit)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            try
            {
                var deletedBy = CurrentUser.UserId ?? Guid.Empty;
                var result = await _repository.DeleteAsync(key, deletedBy);
                return result ? NoContent() : NotFound($"Role with ID {key} not found");
            }
            catch (InvalidOperationException ex)
            {
                // This catches the system role deletion prevention
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Partially updates a role
        /// </summary>
        /// <param name="key">The ID of the role to update</param>
        /// <param name="delta">The role properties to update</param>
        /// <returns>The updated role</returns>
        [RequirePermission(PermissionConstants.RolesEdit)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<RoleEntity> delta)
        {
            try
            {
                _logger.LogInformation($"Received PATCH request for role {key}");

                if (delta == null)
                {
                    _logger.LogWarning($"Update data is null for role {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing role
                var existingRole = await _repository.GetByIdAsync(key);
                if (existingRole == null)
                {
                    return NotFound($"Role with ID {key} was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = RoleMapper.ToEntity(existingRole);
                delta.CopyChangedValues(updatedEntity);

                // Map back to ROLE entity
                existingRole.NAME = updatedEntity.Name;
                existingRole.DISPLAY_NAME = updatedEntity.DisplayName;
                existingRole.DESCRIPTION = updatedEntity.Description;
                existingRole.IS_SYSTEM_ROLE = updatedEntity.IsSystemRole;

                var result = await _repository.UpdateAsync(existingRole);
                return Updated(RoleMapper.ToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role with ID {key}");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }
        
        /// <summary>
        /// Gets all permissions for a role by role name
        /// This allows the frontend to get permissions for the current user based on their role
        /// </summary>
        /// <param name="roleName">The name of the role</param>
        /// <returns>A collection of permissions for the role</returns>
        [EnableQuery]
        [HttpGet("/odata/v1/Roles/GetPermissionsByRoleName")] 
        public async Task<IActionResult> GetPermissionsByRoleName([FromQuery] string roleName)
        {
            try
            {
                if (string.IsNullOrEmpty(roleName))
                {
                    return BadRequest("Role name is required");
                }
                
                // Get all roles and filter by name
                var allRoles = await _repository.GetAllAsync();
                var role = allRoles.FirstOrDefault(r => r.NAME.ToLower() == roleName.ToLower() && r.DELETED == null);
                    
                if (role == null)
                {
                    return NotFound($"Role with name {roleName} not found");
                }
                
                // Get all permissions for this role using the permission repository
                var rolePermissions = await _permissionRepository.GetByRoleIdAsync(role.GUID);
                    
                // Map permissions to a simple model for the frontend
                var permissions = rolePermissions.Select(rp => new
                {
                    Name = rp.PERMISSION,
                    IsViewPermission = rp.PERMISSION.EndsWith(".view"),
                    IsEditPermission = rp.PERMISSION.EndsWith(".edit")
                }).ToList();
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving permissions for role {roleName}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        /// <summary>
        /// Gets all permissions for the current user
        /// This is a convenience method for the frontend to easily get permissions without needing to know the role name
        /// </summary>
        /// <returns>A collection of permissions for the current user</returns>
        [EnableQuery]
        [HttpGet("/odata/v1/Roles/GetCurrentUserPermissions")] 
        public async Task<IActionResult> GetCurrentUserPermissions()
        {
            try
            {
                // Ensure user is fully initialized first using our new async method
                var currentUser = await InitializeCurrentUserAsync();
                
                // Now we can safely access the role
                string? roleName = currentUser.Role;
                
                if (string.IsNullOrEmpty(roleName))
                {
                    return BadRequest("Current user has no assigned role");
                }
                
                // Get all roles and filter by name
                var allRoles = await _repository.GetAllAsync();
                var role = allRoles.FirstOrDefault(r => r.NAME.ToLower() == roleName.ToLower() && r.DELETED == null);
                    
                if (role == null)
                {
                    return NotFound($"Role with name {roleName} not found");
                }
                
                // Use the permissions directly from our initialized currentUser object
                // This is faster than going through the repository again
                if (currentUser.Permissions != null && currentUser.Permissions.Any())
                {
                    var permissions = currentUser.Permissions.Select(p => new
                    {
                        Name = p.Name,
                        IsViewPermission = p.Name.EndsWith(".view"),
                        IsEditPermission = p.Name.EndsWith(".edit")
                    }).ToList();
                    
                    return Ok(permissions);
                }
                
                // Fallback to getting permissions from the repository if not cached in CurrentUser
                var rolePermissions = await _permissionRepository.GetByRoleIdAsync(role.GUID);
                    
                // Map permissions to a simple model for the frontend
                var repoPermissions = rolePermissions.Select(rp => new
                {
                    Name = rp.PERMISSION,
                    IsViewPermission = rp.PERMISSION.EndsWith(".view"),
                    IsEditPermission = rp.PERMISSION.EndsWith(".edit")
                }).ToList();
                
                return Ok(repoPermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for current user");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
