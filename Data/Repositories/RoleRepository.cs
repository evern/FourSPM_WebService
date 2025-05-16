using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    /// <summary>
    /// Repository implementation for managing roles
    /// </summary>
    public class RoleRepository : IRoleRepository
    {
        private readonly FourSPMContext _context;
        
        /// <summary>
        /// Creates a new role repository
        /// </summary>
        /// <param name="context">Database context</param>
        public RoleRepository(FourSPMContext context)
        {
            _context = context;
        }
        
        /// <inheritdoc/>
        public async Task<IEnumerable<ROLE>> GetAllAsync()
        {
            return await _context.ROLEs
                .Where(r => r.DELETED == null)
                .OrderBy(r => r.ROLE_NAME)
                .ToListAsync();
        }
        
        /// <inheritdoc/>
        public async Task<ROLE?> GetByIdAsync(Guid id)
        {
            return await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);
        }
        
        /// <inheritdoc/>
        public async Task<ROLE?> GetByNameAsync(string roleName)
        {
            return await _context.ROLEs
                .FirstOrDefaultAsync(r => r.ROLE_NAME == roleName && r.DELETED == null);
        }
        
        /// <inheritdoc/>
        public async Task<ROLE> CreateAsync(ROLE role)
        {
            role.GUID = role.GUID == Guid.Empty ? Guid.NewGuid() : role.GUID;
            role.CREATED = DateTime.UtcNow;
            
            _context.ROLEs.Add(role);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(role.GUID) ?? role;
        }
        
        /// <inheritdoc/>
        public async Task<ROLE> UpdateAsync(ROLE role)
        {
            var existingRole = await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == role.GUID && r.DELETED == null);
                
            if (existingRole == null)
                throw new KeyNotFoundException($"Role with ID {role.GUID} not found.");
                
            existingRole.ROLE_NAME = role.ROLE_NAME;
            existingRole.DISPLAY_NAME = role.DISPLAY_NAME;
            existingRole.DESCRIPTION = role.DESCRIPTION;
            existingRole.IS_SYSTEM_ROLE = role.IS_SYSTEM_ROLE;
            existingRole.UPDATED = DateTime.UtcNow;
            existingRole.UPDATEDBY = role.UPDATEDBY;
            
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(role.GUID) ?? role;
        }
        
        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var role = await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);
                
            if (role == null)
                return false;
                
            role.DELETED = DateTime.UtcNow;
            role.DELETEDBY = deletedBy;
            
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ROLEs
                .AnyAsync(r => r.GUID == id && r.DELETED == null);
        }
        
        /// <inheritdoc/>
        public async Task<bool> ExistsByNameAsync(string roleName)
        {
            return await _context.ROLEs
                .AnyAsync(r => r.ROLE_NAME == roleName && r.DELETED == null);
        }
    }
}
