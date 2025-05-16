using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Data.Repositories
{
    /// <summary>
    /// Repository implementation for accessing and manipulating role permissions
    /// </summary>
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;
        private readonly ILogger<RolePermissionRepository> _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public RolePermissionRepository(FourSPMContext context, ApplicationUser user, ILogger<RolePermissionRepository> logger)
        {
            _context = context;
            _user = user;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ROLE_PERMISSION>> GetAllAsync()
        {
            return await _context.ROLE_PERMISSIONs
                .Where(rp => rp.DELETED == null)
                .OrderBy(rp => rp.ROLE_NAME)
                .ThenBy(rp => rp.PERMISSION_NAME)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ROLE_PERMISSION?> GetByIdAsync(Guid id)
        {
            return await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == id && rp.DELETED == null);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ROLE_PERMISSION>> GetByRoleNameAsync(string roleName)
        {
            return await _context.ROLE_PERMISSIONs
                .Where(rp => rp.ROLE_NAME == roleName && rp.DELETED == null && rp.IS_GRANTED)
                .OrderBy(rp => rp.PERMISSION_NAME)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPermissionsByRoleNameAsync(string roleName)
        {
            return await _context.ROLE_PERMISSIONs
                .Where(rp => rp.ROLE_NAME == roleName && rp.DELETED == null && rp.IS_GRANTED)
                .Select(rp => rp.PERMISSION_NAME)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> CheckPermissionAsync(string roleName, string permissionName)
        {
            return await _context.ROLE_PERMISSIONs
                .AnyAsync(rp => rp.ROLE_NAME == roleName 
                              && rp.PERMISSION_NAME == permissionName 
                              && rp.DELETED == null 
                              && rp.IS_GRANTED);
        }

        /// <inheritdoc/>
        public async Task<ROLE_PERMISSION> CreateAsync(ROLE_PERMISSION rolePermission)
        {
            // Set GUID if not already set
            if (rolePermission.GUID == Guid.Empty)
            {
                rolePermission.GUID = Guid.NewGuid();
            }
            
            // Set audit fields
            rolePermission.CREATED = DateTime.Now;
            rolePermission.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _logger.LogInformation("Creating role permission: {RoleName} - {PermissionName}", 
                rolePermission.ROLE_NAME, rolePermission.PERMISSION_NAME);
            
            try
            {
                _context.ROLE_PERMISSIONs.Add(rolePermission);
                await _context.SaveChangesAsync();
                return await GetByIdAsync(rolePermission.GUID) ?? rolePermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role permission: {RoleName} - {PermissionName}", 
                    rolePermission.ROLE_NAME, rolePermission.PERMISSION_NAME);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ROLE_PERMISSION> UpdateAsync(ROLE_PERMISSION rolePermission)
        {
            var existingPermission = await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == rolePermission.GUID && rp.DELETED == null);

            if (existingPermission == null)
            {
                _logger.LogWarning("Attempted to update non-existent role permission: {Id}", rolePermission.GUID);
                throw new KeyNotFoundException($"Role permission with ID {rolePermission.GUID} not found");
            }

            // Update properties
            existingPermission.ROLE_NAME = rolePermission.ROLE_NAME;
            existingPermission.PERMISSION_NAME = rolePermission.PERMISSION_NAME;
            existingPermission.IS_GRANTED = rolePermission.IS_GRANTED;
            existingPermission.DESCRIPTION = rolePermission.DESCRIPTION;
            
            // Set audit fields
            existingPermission.UPDATED = DateTime.Now;
            existingPermission.UPDATEDBY = _user.UserId ?? Guid.Empty;

            _logger.LogInformation("Updating role permission: {Id} - {RoleName} - {PermissionName}", 
                existingPermission.GUID, existingPermission.ROLE_NAME, existingPermission.PERMISSION_NAME);
            
            try
            {
                await _context.SaveChangesAsync();
                return existingPermission;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating role permission: {Id}", rolePermission.GUID);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permission: {Id}", rolePermission.GUID);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var rolePermission = await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == id && rp.DELETED == null);

            if (rolePermission == null)
            {
                _logger.LogWarning("Attempted to delete non-existent role permission: {Id}", id);
                return false;
            }

            // Set deletion audit fields
            rolePermission.DELETED = DateTime.Now;
            rolePermission.DELETEDBY = deletedBy;

            _logger.LogInformation("Deleting role permission: {Id} - {RoleName} - {PermissionName}", 
                rolePermission.GUID, rolePermission.ROLE_NAME, rolePermission.PERMISSION_NAME);
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role permission: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ROLE_PERMISSIONs
                .AnyAsync(rp => rp.GUID == id && rp.DELETED == null);
        }
    }
}
