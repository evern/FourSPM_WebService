using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;
using FourSPM_WebService.Config;
using FourSPM_WebService.Authorization;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// OData controller for role permission management
    /// </summary>
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class RolePermissionsController : FourSPMODataController
    {
        private readonly IRolePermissionRepository _repository;
        private readonly ApplicationUser _applicationUser;
        private readonly ILogger<RolePermissionsController> _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public RolePermissionsController(
            IRolePermissionRepository repository,
            ApplicationUser applicationUser,
            ILogger<RolePermissionsController> logger)
        {
            _repository = repository;
            _applicationUser = applicationUser;
            _logger = logger;
        }

        /// <summary>
        /// Gets all role permissions
        /// </summary>
        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadRolePermissions)]
        public async Task<IActionResult> Get()
        {
            try
            {
                var rolePermissions = await _repository.GetAllAsync();
                var entities = rolePermissions.Select(rp => MapToEntity(rp));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permissions");
                return StatusCode(500, "An error occurred while retrieving role permissions");
            }
        }

        /// <summary>
        /// Gets a specific role permission by ID
        /// </summary>
        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadRolePermissions)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var rolePermission = await _repository.GetByIdAsync(key);
                if (rolePermission == null)
                    return NotFound();

                return Ok(MapToEntity(rolePermission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permission with ID: {Id}", key);
                return StatusCode(500, "An error occurred while retrieving the role permission");
            }
        }

        /// <summary>
        /// Gets all permissions for a specific role
        /// </summary>
        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadRolePermissions)]
        [HttpGet("odata/v1/RolePermissions/ByRole(roleName={roleName})")]
        public async Task<IActionResult> GetByRole([FromRoute] string roleName)
        {
            try
            {
                if (string.IsNullOrEmpty(roleName))
                    return BadRequest("Role name cannot be empty");

                var rolePermissions = await _repository.GetByRoleNameAsync(roleName);
                var entities = rolePermissions.Select(rp => MapToEntity(rp));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for role: {RoleName}", roleName);
                return StatusCode(500, "An error occurred while retrieving permissions for the specified role");
            }
        }

        /// <summary>
        /// Creates a new role permission
        /// </summary>
        [RequirePermission(AuthConstants.Permissions.WriteRolePermissions)]
        public async Task<IActionResult> Post([FromBody] RolePermissionEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrEmpty(entity.RoleName))
                    return BadRequest("Role name cannot be empty");

                if (string.IsNullOrEmpty(entity.PermissionName))
                    return BadRequest("Permission name cannot be empty");

                var rolePermission = new ROLE_PERMISSION
                {
                    GUID = Guid.NewGuid(),
                    ROLE_NAME = entity.RoleName,
                    PERMISSION_NAME = entity.PermissionName,
                    IS_GRANTED = entity.IsGranted,
                    DESCRIPTION = entity.Description
                };

                var created = await _repository.CreateAsync(rolePermission);
                return Created(MapToEntity(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role permission: {RoleName} - {PermissionName}", 
                    entity.RoleName, entity.PermissionName);
                return StatusCode(500, "An error occurred while creating the role permission");
            }
        }

        /// <summary>
        /// Updates an existing role permission
        /// </summary>
        [RequirePermission(AuthConstants.Permissions.WriteRolePermissions)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] RolePermissionEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (key != entity.Guid)
                    return BadRequest("ID mismatch between URL and request body");

                if (string.IsNullOrEmpty(entity.RoleName))
                    return BadRequest("Role name cannot be empty");

                if (string.IsNullOrEmpty(entity.PermissionName))
                    return BadRequest("Permission name cannot be empty");

                // Check if the entity exists
                var existing = await _repository.GetByIdAsync(key);
                if (existing == null)
                    return NotFound();

                // Update the entity
                existing.ROLE_NAME = entity.RoleName;
                existing.PERMISSION_NAME = entity.PermissionName;
                existing.IS_GRANTED = entity.IsGranted;
                existing.DESCRIPTION = entity.Description;

                var updated = await _repository.UpdateAsync(existing);
                return Updated(MapToEntity(updated));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permission with ID: {Id}", key);
                return StatusCode(500, "An error occurred while updating the role permission");
            }
        }

        /// <summary>
        /// Deletes a role permission
        /// </summary>
        [RequirePermission(AuthConstants.Permissions.WriteRolePermissions)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            try
            {
                if (key == Guid.Empty)
                    return BadRequest("Invalid ID");

                // Use the current user's ID for the delete operation
                var userId = _applicationUser.UserId ?? Guid.Empty;
                if (userId == Guid.Empty)
                    return Unauthorized("User ID is required for deletion");

                var result = await _repository.DeleteAsync(key, userId);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role permission with ID: {Id}", key);
                return StatusCode(500, "An error occurred while deleting the role permission");
            }
        }

        /// <summary>
        /// Partially updates a role permission
        /// </summary>
        [RequirePermission(AuthConstants.Permissions.WriteRolePermissions)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<RolePermissionEntity> delta)
        {
            try
            {
                if (key == Guid.Empty)
                    return BadRequest("Invalid ID");

                if (delta == null)
                    return BadRequest("Update data cannot be null");

                // Get the existing role permission
                var existing = await _repository.GetByIdAsync(key);
                if (existing == null)
                    return NotFound();

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existing);
                delta.CopyChangedValues(updatedEntity);

                // Validate the updated entity
                if (string.IsNullOrEmpty(updatedEntity.RoleName))
                    return BadRequest("Role name cannot be empty");

                if (string.IsNullOrEmpty(updatedEntity.PermissionName))
                    return BadRequest("Permission name cannot be empty");

                // Map back to the database entity
                existing.ROLE_NAME = updatedEntity.RoleName;
                existing.PERMISSION_NAME = updatedEntity.PermissionName;
                existing.IS_GRANTED = updatedEntity.IsGranted;
                existing.DESCRIPTION = updatedEntity.Description;

                var result = await _repository.UpdateAsync(existing);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching role permission with ID: {Id}", key);
                return StatusCode(500, "An error occurred while updating the role permission");
            }
        }

        /// <summary>
        /// Check if a role has a specific permission
        /// </summary>
        [RequirePermission(AuthConstants.Permissions.ReadRolePermissions)]
        [HttpGet("odata/v1/RolePermissions/CheckPermission(roleName={roleName},permissionName={permissionName})")]
        public async Task<IActionResult> CheckPermission([FromRoute] string roleName, [FromRoute] string permissionName)
        {
            try
            {
                if (string.IsNullOrEmpty(roleName))
                    return BadRequest("Role name cannot be empty");

                if (string.IsNullOrEmpty(permissionName))
                    return BadRequest("Permission name cannot be empty");

                var hasPermission = await _repository.CheckPermissionAsync(roleName, permissionName);
                return Ok(new { HasPermission = hasPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for role {Role}", 
                    permissionName, roleName);
                return StatusCode(500, "An error occurred while checking the permission");
            }
        }

        /// <summary>
        /// Maps a database entity to an OData entity
        /// </summary>
        private static RolePermissionEntity MapToEntity(ROLE_PERMISSION rolePermission)
        {
            return new RolePermissionEntity
            {
                Guid = rolePermission.GUID,
                RoleName = rolePermission.ROLE_NAME,
                PermissionName = rolePermission.PERMISSION_NAME,
                IsGranted = rolePermission.IS_GRANTED,
                Description = rolePermission.DESCRIPTION,
                Created = rolePermission.CREATED,
                CreatedBy = rolePermission.CREATEDBY,
                Updated = rolePermission.UPDATED,
                UpdatedBy = rolePermission.UPDATEDBY,
                Deleted = rolePermission.DELETED,
                DeletedBy = rolePermission.DELETEDBY
            };
        }
    }
}
