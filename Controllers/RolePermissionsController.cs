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

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class RolePermissionsController : FourSPMODataController
    {
        private readonly IRolePermissionRepository _repository;
        private readonly ApplicationUser _applicationUser;
        private readonly ILogger<RolePermissionsController> _logger;

        public RolePermissionsController(
            IRolePermissionRepository repository,
            ApplicationUser applicationUser,
            ILogger<RolePermissionsController> logger)
        {
            _repository = repository;
            _applicationUser = applicationUser;
            _logger = logger;
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            try
            {
                var rolePermissions = await _repository.GetAllAsync();
                var entities = rolePermissions.Select(rp => RolePermissionMapper.ToEntity(rp));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permissions");
                return StatusCode(500, "Internal server error");
            }
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] int key)
        {
            try
            {
                var rolePermission = await _repository.GetByIdAsync(key);
                if (rolePermission == null)
                    return NotFound();

                return Ok(RolePermissionMapper.ToEntity(rolePermission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role permission with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [EnableQuery]
        [HttpGet("GetByRoleId(roleId={roleId})")]
        public async Task<IActionResult> GetByRoleId([FromODataUri] int roleId)
        {
            try
            {
                var rolePermissions = await _repository.GetByRoleIdAsync(roleId);
                var entities = rolePermissions.Select(rp => RolePermissionMapper.ToEntity(rp));
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role permissions for role ID {roleId}");
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> Post([FromBody] RolePermissionEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var rolePermission = new ROLE_PERMISSION
                {
                    GUID = entity.Guid,
                    GUID_ROLE = entity.RoleGuid,
                    PERMISSION = entity.Permission,
                    CREATEDBY = _applicationUser.UserName ?? string.Empty
                };

                var result = await _repository.CreateAsync(rolePermission);
                return Created(RolePermissionMapper.ToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role permission");
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> Put([FromRoute] int key, [FromBody] RolePermissionEntity entity)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (key != entity.Guid)
                    return BadRequest("The ID in the URL must match the ID in the request body");

                var rolePermission = new ROLE_PERMISSION
                {
                    GUID = entity.Guid,
                    GUID_ROLE = entity.RoleGuid,
                    PERMISSION = entity.Permission,
                    CREATEDBY = _applicationUser.UserName ?? string.Empty
                };

                var result = await _repository.UpdateAsync(rolePermission);
                if (result == null)
                    return NotFound($"Role permission with ID {key} not found");
                    
                return Updated(RolePermissionMapper.ToEntity(result));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role permission with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }

        public async Task<IActionResult> Delete([FromRoute] int key)
        {
            try
            {
                var deletedBy = _applicationUser.UserName ?? string.Empty;
                var result = await _repository.DeleteAsync(key, deletedBy);
                return result ? NoContent() : NotFound($"Role permission with ID {key} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role permission with ID {key}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Partially updates a role permission
        /// </summary>
        /// <param name="key">The ID of the role permission to update</param>
        /// <param name="delta">The role permission properties to update</param>
        /// <returns>The updated role permission</returns>
        public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<RolePermissionEntity> delta)
        {
            try
            {
                _logger.LogInformation($"Received PATCH request for role permission {key}");

                if (delta == null)
                {
                    _logger.LogWarning($"Update data is null for role permission {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing role permission
                var existingRolePermission = await _repository.GetByIdAsync(key);
                if (existingRolePermission == null)
                {
                    return NotFound($"Role permission with ID {key} was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = RolePermissionMapper.ToEntity(existingRolePermission);
                delta.CopyChangedValues(updatedEntity);

                // Map back to ROLE_PERMISSION entity
                existingRolePermission.GUID_ROLE = updatedEntity.RoleGuid;
                existingRolePermission.PERMISSION = updatedEntity.Permission;

                var result = await _repository.UpdateAsync(existingRolePermission);
                return Updated(RolePermissionMapper.ToEntity(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role permission with ID {key}");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }
    }
}
