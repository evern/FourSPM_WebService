using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    /// <summary>
    /// Repository interface for accessing and manipulating role permissions
    /// </summary>
    public interface IRolePermissionRepository
    {
        /// <summary>
        /// Gets all non-deleted role permissions
        /// </summary>
        Task<IEnumerable<ROLE_PERMISSION>> GetAllAsync();
        
        /// <summary>
        /// Gets a specific role permission by ID
        /// </summary>
        Task<ROLE_PERMISSION?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all permissions for a specific role
        /// </summary>
        Task<IEnumerable<ROLE_PERMISSION>> GetByRoleNameAsync(string roleName);
        
        /// <summary>
        /// Gets all permission names for a specific role
        /// </summary>
        Task<IEnumerable<string>> GetPermissionsByRoleNameAsync(string roleName);
        
        /// <summary>
        /// Checks if a role has a specific permission
        /// </summary>
        Task<bool> CheckPermissionAsync(string roleName, string permissionName);
        
        /// <summary>
        /// Creates a new role permission
        /// </summary>
        Task<ROLE_PERMISSION> CreateAsync(ROLE_PERMISSION rolePermission);
        
        /// <summary>
        /// Updates an existing role permission
        /// </summary>
        Task<ROLE_PERMISSION> UpdateAsync(ROLE_PERMISSION rolePermission);
        
        /// <summary>
        /// Soft deletes a role permission
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        
        /// <summary>
        /// Checks if a role permission exists and is not deleted
        /// </summary>
        Task<bool> ExistsAsync(Guid id);
    }
}
