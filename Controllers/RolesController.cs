using System;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Authorization;
using FourSPM_WebService.Config;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// OData controller for role management
    /// </summary>
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class RolesController : FourSPMODataController
    {
        private readonly IRoleRepository _repository;
        private readonly ApplicationUser _applicationUser;
        private readonly ILogger<RolesController> _logger;
        
        /// <summary>
        /// Creates a new roles controller
        /// </summary>
        /// <param name="repository">Role repository</param>
        /// <param name="applicationUser">Current user</param>
        /// <param name="logger">Logger</param>
        public RolesController(
            IRoleRepository repository,
            ApplicationUser applicationUser,
            ILogger<RolesController> logger)
        {
            _repository = repository;
            _applicationUser = applicationUser;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets all roles
        /// </summary>
        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadRoles)]
        public async Task<IActionResult> Get()
        {
            try
            {
                var roles = await _repository.GetAllAsync();
                var entities = roles.Select(r => MapToEntity(r));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, "An error occurred while retrieving roles");
            }
        }
        
        /// <summary>
        /// Gets a specific role by ID
        /// </summary>
        /// <param name="key">Role ID</param>
        [EnableQuery]
        [RequirePermission(AuthConstants.Permissions.ReadRoles)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            try
            {
                var role = await _repository.GetByIdAsync(key);
                if (role == null)
                    return NotFound();
                    
                return Ok(MapToEntity(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID: {Id}", key);
                return StatusCode(500, "An error occurred while retrieving the role");
            }
        }
        
        /// <summary>
        /// Creates a new role
        /// </summary>
        /// <param name="entity">Role data</param>
        [RequirePermission(AuthConstants.Permissions.WriteRoles)]
        public async Task<IActionResult> Post([FromBody] RoleEntity entity)
        {
            try
            {
                if (entity == null)
                    return BadRequest("Role data cannot be null");
                    
                if (string.IsNullOrEmpty(entity.RoleName))
                    return BadRequest("Role name cannot be empty");
                    
                if (await _repository.ExistsByNameAsync(entity.RoleName))
                    return Conflict($"A role with name '{entity.RoleName}' already exists");
                    
                var role = new ROLE
                {
                    GUID = Guid.NewGuid(),
                    ROLE_NAME = entity.RoleName,
                    DISPLAY_NAME = entity.DisplayName,
                    DESCRIPTION = entity.Description,
                    IS_SYSTEM_ROLE = entity.IsSystemRole,
                    CREATEDBY = _applicationUser.UserId!.Value
                };
                
                var result = await _repository.CreateAsync(role);
                return Created(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, "An error occurred while creating the role");
            }
        }
        
        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <param name="key">Role ID</param>
        /// <param name="entity">Updated role data</param>
        [RequirePermission(AuthConstants.Permissions.WriteRoles)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] RoleEntity entity)
        {
            try
            {
                if (entity == null)
                    return BadRequest("Role data cannot be null");
                    
                if (string.IsNullOrEmpty(entity.RoleName))
                    return BadRequest("Role name cannot be empty");
                    
                var existingRole = await _repository.GetByIdAsync(key);
                if (existingRole == null)
                    return NotFound();
                    
                // Check if the updated role name conflicts with another role
                if (existingRole.ROLE_NAME != entity.RoleName &&
                    await _repository.ExistsByNameAsync(entity.RoleName))
                    return Conflict($"A role with name '{entity.RoleName}' already exists");
                    
                existingRole.ROLE_NAME = entity.RoleName;
                existingRole.DISPLAY_NAME = entity.DisplayName;
                existingRole.DESCRIPTION = entity.Description;
                existingRole.IS_SYSTEM_ROLE = entity.IsSystemRole;
                existingRole.UPDATEDBY = _applicationUser.UserId!.Value;
                
                var result = await _repository.UpdateAsync(existingRole);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID: {Id}", key);
                return StatusCode(500, "An error occurred while updating the role");
            }
        }
        
        /// <summary>
        /// Deletes a role
        /// </summary>
        /// <param name="key">Role ID</param>
        [RequirePermission(AuthConstants.Permissions.WriteRoles)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            try
            {
                var role = await _repository.GetByIdAsync(key);
                if (role == null)
                    return NotFound();
                    
                if (role.IS_SYSTEM_ROLE)
                    return BadRequest("System roles cannot be deleted");
                    
                var result = await _repository.DeleteAsync(key, _applicationUser.UserId!.Value);
                return result ? NoContent() : StatusCode(500, "Failed to delete the role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID: {Id}", key);
                return StatusCode(500, "An error occurred while deleting the role");
            }
        }
        
        /// <summary>
        /// Partially updates a role
        /// </summary>
        /// <param name="key">Role ID</param>
        /// <param name="delta">Changes to apply</param>
        [RequirePermission(AuthConstants.Permissions.WriteRoles)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<RoleEntity> delta)
        {
            try
            {
                if (delta == null)
                    return BadRequest("Update data cannot be null");
                    
                var existingRole = await _repository.GetByIdAsync(key);
                if (existingRole == null)
                    return NotFound();
                    
                if (existingRole.IS_SYSTEM_ROLE)
                    return BadRequest("System roles cannot be modified");
                    
                // Convert to entity for patching
                var roleEntity = MapToEntity(existingRole);
                
                // Apply changes
                delta.Patch(roleEntity);
                
                // Validate
                if (string.IsNullOrEmpty(roleEntity.RoleName))
                    return BadRequest("Role name cannot be empty");
                    
                // Check for role name conflicts
                if (existingRole.ROLE_NAME != roleEntity.RoleName && 
                    await _repository.ExistsByNameAsync(roleEntity.RoleName))
                    return Conflict($"A role with name '{roleEntity.RoleName}' already exists");
                    
                // Update the database entity
                existingRole.ROLE_NAME = roleEntity.RoleName;
                existingRole.DISPLAY_NAME = roleEntity.DisplayName;
                existingRole.DESCRIPTION = roleEntity.Description;
                existingRole.IS_SYSTEM_ROLE = roleEntity.IsSystemRole;
                existingRole.UPDATEDBY = _applicationUser.UserId!.Value;
                
                var result = await _repository.UpdateAsync(existingRole);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching role with ID: {Id}", key);
                return StatusCode(500, "An error occurred while updating the role");
            }
        }
        
        /// <summary>
        /// Maps a database entity to an OData entity
        /// </summary>
        /// <param name="role">Database entity</param>
        /// <returns>OData entity</returns>
        private static RoleEntity MapToEntity(ROLE role)
        {
            return new RoleEntity
            {
                Guid = role.GUID,
                RoleName = role.ROLE_NAME,
                DisplayName = role.DISPLAY_NAME,
                Description = role.DESCRIPTION,
                IsSystemRole = role.IS_SYSTEM_ROLE,
                Created = role.CREATED,
                CreatedBy = role.CREATEDBY,
                Updated = role.UPDATED,
                UpdatedBy = role.UPDATEDBY,
                Deleted = role.DELETED,
                DeletedBy = role.DELETEDBY
            };
        }
    }
}
