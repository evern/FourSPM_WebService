using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public RolePermissionRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<ROLE_PERMISSION>> GetAllAsync()
        {
            return await _context.ROLE_PERMISSIONs
                .Where(rp => rp.DELETED == null)
                .OrderByDescending(rp => rp.CREATED)
                .ToListAsync();
        }

        public async Task<ROLE_PERMISSION?> GetByIdAsync(int id)
        {
            return await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == id && rp.DELETED == null);
        }

        public async Task<IEnumerable<ROLE_PERMISSION>> GetByRoleIdAsync(int roleId)
        {
            return await _context.ROLE_PERMISSIONs
                .Where(rp => rp.GUID_ROLE == roleId && rp.DELETED == null)
                .OrderByDescending(rp => rp.CREATED)
                .ToListAsync();
        }

        public async Task<ROLE_PERMISSION> CreateAsync(ROLE_PERMISSION rolePermission)
        {
            rolePermission.CREATED = DateTime.Now;
            rolePermission.CREATEDBY = _user.UserName ?? string.Empty;
            
            _context.ROLE_PERMISSIONs.Add(rolePermission);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(rolePermission.GUID) ?? rolePermission;
        }

        public async Task<ROLE_PERMISSION> UpdateAsync(ROLE_PERMISSION rolePermission)
        {
            var existingRolePermission = await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == rolePermission.GUID && rp.DELETED == null);

            if (existingRolePermission == null)
                return null!;

            // Update properties
            existingRolePermission.GUID_ROLE = rolePermission.GUID_ROLE;
            existingRolePermission.PERMISSION = rolePermission.PERMISSION;
            
            // Update audit fields
            existingRolePermission.UPDATED = DateTime.Now;
            existingRolePermission.UPDATEDBY = _user.UserName ?? string.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return existingRolePermission;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.ROLE_PERMISSIONs.AnyAsync(rp => rp.GUID == rolePermission.GUID && rp.DELETED == null))
                {
                    throw new KeyNotFoundException($"Role permission with ID {rolePermission.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(int id, string deletedBy)
        {
            var rolePermission = await _context.ROLE_PERMISSIONs
                .FirstOrDefaultAsync(rp => rp.GUID == id && rp.DELETED == null);

            if (rolePermission == null)
                return false;

            rolePermission.DELETED = DateTime.Now;
            rolePermission.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ROLE_PERMISSIONs
                .AnyAsync(rp => rp.GUID == id && rp.DELETED == null);
        }
    }
}
