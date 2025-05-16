using FourSPM_WebService.Authorization;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Controllers
{
    /// <summary>
    /// API controller for managing roles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly FourSPMContext _context;
        private readonly ILogger<RolesController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesController"/> class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">The logger</param>
        public RolesController(FourSPMContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all available permissions defined in the system
        /// </summary>
        /// <returns>A list of all permissions grouped by category</returns>
        [HttpGet("permissions")]
        [RequirePermission(Permissions.ViewRoles)]
        public ActionResult<IEnumerable<PermissionDto>> GetAllPermissions()
        {
            try
            {
                var permissions = Permissions.GetPermissionsWithCategories()
                    .Select(p => new PermissionDto
                    {
                        Name = p.Permission,
                        Category = p.Category,
                        IsGranted = false // Not relevant for this endpoint, but included for consistency
                    })
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToList();
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all permissions");
                return StatusCode(500, new { message = "An error occurred while retrieving permissions" });
            }
        }

        /// <summary>
        /// Gets all roles
        /// </summary>
        /// <returns>A list of roles</returns>
        [HttpGet]
        [RequirePermission(Permissions.ViewRoles)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            try
            {
                var roles = await _context.ROLEs
                    .Where(r => r.DELETED == null)
                    .Select(r => new RoleDto
                    {
                        Guid = r.GUID,
                        Name = r.NAME,
                        DisplayName = r.DISPLAY_NAME,
                        Description = r.DESCRIPTION,
                        IsSystemRole = r.IS_SYSTEM_ROLE
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { message = "An error occurred while retrieving roles" });
            }
        }

        /// <summary>
        /// Gets a specific role by ID
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>The role</returns>
        [HttpGet("{id}")]
        [RequirePermission(Permissions.ViewRoles)]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            try
            {
                var role = await _context.ROLEs
                    .Include(r => r.ROLE_PERMISSIONs.Where(rp => rp.DELETED == null))
                    .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);

                if (role == null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                var roleDto = new RoleDto
                {
                    Guid = role.GUID,
                    Name = role.NAME,
                    DisplayName = role.DISPLAY_NAME,
                    Description = role.DESCRIPTION,
                    IsSystemRole = role.IS_SYSTEM_ROLE,
                    Permissions = role.ROLE_PERMISSIONs.Select(rp => rp.PERMISSION).ToList()
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role with ID {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving the role" });
            }
        }

        /// <summary>
        /// Creates a new role
        /// </summary>
        /// <param name="createRoleDto">The role data</param>
        /// <returns>The created role</returns>
        [HttpPost]
        [RequirePermission(Permissions.EditRoles)]
        public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto createRoleDto)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(createRoleDto.Name))
                {
                    return BadRequest(new { message = "Role name is required" });
                }

                if (string.IsNullOrWhiteSpace(createRoleDto.DisplayName))
                {
                    return BadRequest(new { message = "Role display name is required" });
                }

                // Check if role with same name already exists
                if (await _context.ROLEs.AnyAsync(r => r.NAME == createRoleDto.Name && r.DELETED == null))
                {
                    return BadRequest(new { message = "Role with this name already exists" });
                }

                // Create new role
                var role = new ROLE
                {
                    NAME = createRoleDto.Name,
                    DISPLAY_NAME = createRoleDto.DisplayName,
                    DESCRIPTION = createRoleDto.Description,
                    IS_SYSTEM_ROLE = createRoleDto.IsSystemRole,
                    CREATED = DateTime.UtcNow,
                    CREATEDBY = User.Identity?.Name ?? "System"
                };

                _context.ROLEs.Add(role);
                await _context.SaveChangesAsync();

                // Add permissions if specified
                if (createRoleDto.Permissions != null && createRoleDto.Permissions.Any())
                {
                    foreach (var permission in createRoleDto.Permissions)
                    {
                        _context.ROLE_PERMISSIONs.Add(new ROLE_PERMISSION
                        {
                            GUID_ROLE = role.GUID,
                            PERMISSION = permission,
                            CREATED = DateTime.UtcNow,
                            CREATEDBY = User.Identity?.Name ?? "System"
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                // Return created role with assigned permissions
                var roleDto = new RoleDto
                {
                    Guid = role.GUID,
                    Name = role.NAME,
                    DisplayName = role.DISPLAY_NAME,
                    Description = role.DESCRIPTION,
                    IsSystemRole = role.IS_SYSTEM_ROLE,
                    Permissions = createRoleDto.Permissions
                };

                return CreatedAtAction(nameof(GetRole), new { id = role.GUID }, roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new { message = "An error occurred while creating the role" });
            }
        }

        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <param name="updateRoleDto">The role data to update</param>
        /// <returns>No content</returns>
        [HttpPut("{id}")]
        [RequirePermission(Permissions.EditRoles)]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto updateRoleDto)
        {
            try
            {
                var role = await _context.ROLEs.FindAsync(id);

                if (role == null || role.DELETED != null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                // Check if trying to update a system role
                if (role.IS_SYSTEM_ROLE && !User.IsInRole("Administrator"))
                {
                    // Return 403 Forbidden with a message
                    return StatusCode(403, new { message = "Only administrators can update system roles" });
                }

                // Check name uniqueness if updating name
                if (!string.IsNullOrWhiteSpace(updateRoleDto.Name) && 
                    updateRoleDto.Name != role.NAME && 
                    await _context.ROLEs.AnyAsync(r => r.NAME == updateRoleDto.Name && r.DELETED == null))
                {
                    return BadRequest(new { message = "Role with this name already exists" });
                }

                // Update role properties
                if (!string.IsNullOrWhiteSpace(updateRoleDto.Name))
                {
                    role.NAME = updateRoleDto.Name;
                }

                if (!string.IsNullOrWhiteSpace(updateRoleDto.DisplayName))
                {
                    role.DISPLAY_NAME = updateRoleDto.DisplayName;
                }

                role.DESCRIPTION = updateRoleDto.Description ?? role.DESCRIPTION;

                if (updateRoleDto.IsSystemRole.HasValue)
                {
                    role.IS_SYSTEM_ROLE = updateRoleDto.IsSystemRole.Value;
                }

                role.UPDATED = DateTime.UtcNow;
                role.UPDATEDBY = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role with ID {id}");
                return StatusCode(500, new { message = "An error occurred while updating the role" });
            }
        }

        /// <summary>
        /// Deletes a role
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [RequirePermission(Permissions.EditRoles)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var role = await _context.ROLEs.FindAsync(id);

                if (role == null || role.DELETED != null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                // Prevent deletion of system roles
                if (role.IS_SYSTEM_ROLE)
                {
                    return BadRequest(new { message = "System roles cannot be deleted" });
                }

                // Soft delete
                role.DELETED = DateTime.UtcNow;
                role.DELETEDBY = User.Identity?.Name ?? "System";

                // Soft delete all associated permissions
                var rolePermissions = await _context.ROLE_PERMISSIONs
                    .Where(rp => rp.GUID_ROLE == id && rp.DELETED == null)
                    .ToListAsync();

                foreach (var permission in rolePermissions)
                {
                    permission.DELETED = DateTime.UtcNow;
                    permission.DELETEDBY = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role with ID {id}");
                return StatusCode(500, new { message = "An error occurred while deleting the role" });
            }
        }

        /// <summary>
        /// Gets all permissions for a role
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>The list of permissions</returns>
        [HttpGet("{id}/permissions")]
        [RequirePermission(Permissions.ViewRoles)]
        public async Task<ActionResult<RolePermissionsDto>> GetRolePermissions(int id)
        {
            try
            {
                var role = await _context.ROLEs
                    .Include(r => r.ROLE_PERMISSIONs.Where(rp => rp.DELETED == null))
                    .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);

                if (role == null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                // Get all available permissions and mark those that are granted to this role
                var allPermissions = Permissions.GetAllPermissions().ToList();
                var grantedPermissions = role.ROLE_PERMISSIONs.Select(rp => rp.PERMISSION).ToList();
                
                var permissionDtos = allPermissions
                    .Select(p => new PermissionDto
                    {
                        Name = p,
                        Category = p.Split('.')[0],
                        IsGranted = grantedPermissions.Contains(p) || role.IS_SYSTEM_ROLE
                    })
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToList();
                
                var rolePermissions = new RolePermissionsDto
                {
                    RoleGuid = role.GUID,
                    RoleName = role.NAME,
                    IsSystemRole = role.IS_SYSTEM_ROLE,
                    Permissions = grantedPermissions
                };

                return Ok(new
                {
                    role = new
                    {
                        id = role.GUID,
                        name = role.NAME,
                        displayName = role.DISPLAY_NAME,
                        isSystemRole = role.IS_SYSTEM_ROLE
                    },
                    permissions = permissionDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving permissions for role with ID {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving role permissions" });
            }
        }

        /// <summary>
        /// Updates the permissions for a role
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <param name="updateDto">The permissions update data</param>
        /// <returns>No content</returns>
        [HttpPut("{id}/permissions")]
        [RequirePermission(Permissions.EditRoles)]
        public async Task<IActionResult> UpdateRolePermissions(int id, UpdateRolePermissionsDto updateDto)
        {
            try
            {
                // Validate input
                if (updateDto.Permissions == null)
                {
                    return BadRequest(new { message = "Permissions list is required" });
                }
                
                // Validate that all permissions exist
                var allPermissions = Permissions.GetAllPermissions().ToList();
                var invalidPermissions = updateDto.Permissions.Where(p => !allPermissions.Contains(p)).ToList();
                if (invalidPermissions.Any())
                {
                    return BadRequest(new { message = $"The following permissions are invalid: {string.Join(", ", invalidPermissions)}" });
                }

                var role = await _context.ROLEs.FindAsync(id);

                if (role == null || role.DELETED != null)
                {
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                // Only administrators can update system role permissions
                if (role.IS_SYSTEM_ROLE && !User.IsInRole("Administrator"))
                {
                    // Return 403 Forbidden with a message
                    return StatusCode(403, new { message = "Only administrators can update system role permissions" });
                }

                // Get current permissions
                var currentPermissions = await _context.ROLE_PERMISSIONs
                    .Where(rp => rp.GUID_ROLE == id && rp.DELETED == null)
                    .ToListAsync();

                // Auto-grant view permissions when edit permissions are granted
                var permissionsToGrant = new HashSet<string>(updateDto.Permissions);
                foreach (var permission in updateDto.Permissions.Where(p => p.EndsWith(".Edit") || p.EndsWith(".Delete") || p.EndsWith(".Approve")))
                {
                    string? viewPermission = Permissions.GetImpliedViewPermission(permission);
                    if (!string.IsNullOrEmpty(viewPermission) && allPermissions.Contains(viewPermission))
                    {
                        permissionsToGrant.Add(viewPermission);
                    }
                }
                
                // Get permissions to add
                var permissionsToAdd = permissionsToGrant
                    .Where(p => !currentPermissions.Any(cp => cp.PERMISSION == p))
                    .ToList();

                // Get permissions to remove
                var permissionsToRemove = currentPermissions
                    .Where(cp => !permissionsToGrant.Contains(cp.PERMISSION))
                    .ToList();

                // Add new permissions
                foreach (var permission in permissionsToAdd)
                {
                    _context.ROLE_PERMISSIONs.Add(new ROLE_PERMISSION
                    {
                        GUID_ROLE = id,
                        PERMISSION = permission,
                        CREATED = DateTime.UtcNow,
                        CREATEDBY = User.Identity?.Name ?? "System"
                    });
                }

                // Soft delete removed permissions
                foreach (var permission in permissionsToRemove)
                {
                    permission.DELETED = DateTime.UtcNow;
                    permission.DELETEDBY = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating permissions for role with ID {id}");
                return StatusCode(500, new { message = "An error occurred while updating role permissions" });
            }
        }
    }
}
