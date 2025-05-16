using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    /// <summary>
    /// Repository interface for managing roles
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Gets all roles that are not deleted
        /// </summary>
        /// <returns>Collection of roles</returns>
        Task<IEnumerable<ROLE>> GetAllAsync();
        
        /// <summary>
        /// Gets a role by its ID
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>Role if found, null otherwise</returns>
        Task<ROLE?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets a role by its name
        /// </summary>
        /// <param name="roleName">Role name</param>
        /// <returns>Role if found, null otherwise</returns>
        Task<ROLE?> GetByNameAsync(string roleName);
        
        /// <summary>
        /// Creates a new role
        /// </summary>
        /// <param name="role">Role to create</param>
        /// <returns>Created role</returns>
        Task<ROLE> CreateAsync(ROLE role);
        
        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <param name="role">Role with updated values</param>
        /// <returns>Updated role</returns>
        Task<ROLE> UpdateAsync(ROLE role);
        
        /// <summary>
        /// Deletes a role (soft delete)
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <param name="deletedBy">ID of the user performing the deletion</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        
        /// <summary>
        /// Checks if a role exists
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>True if role exists, false otherwise</returns>
        Task<bool> ExistsAsync(Guid id);
        
        /// <summary>
        /// Checks if a role name exists
        /// </summary>
        /// <param name="roleName">Role name</param>
        /// <returns>True if role name exists, false otherwise</returns>
        Task<bool> ExistsByNameAsync(string roleName);
    }
}
