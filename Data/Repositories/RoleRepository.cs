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
    public class RoleRepository : IRoleRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public RoleRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<ROLE>> GetAllAsync()
        {
            return await _context.ROLEs
                .Where(r => r.DELETED == null)
                .OrderByDescending(r => r.CREATED)
                .ToListAsync();
        }

        public async Task<ROLE?> GetByIdAsync(int id)
        {
            return await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);
        }

        public async Task<ROLE> CreateAsync(ROLE role)
        {
            role.CREATED = DateTime.Now;
            role.CREATEDBY = _user.UserName ?? string.Empty;
            
            _context.ROLEs.Add(role);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(role.GUID) ?? role;
        }

        public async Task<ROLE> UpdateAsync(ROLE role)
        {
            var existingRole = await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == role.GUID && r.DELETED == null);

            if (existingRole == null)
                return null!;

            // Update properties
            existingRole.NAME = role.NAME;
            existingRole.DISPLAY_NAME = role.DISPLAY_NAME;
            existingRole.DESCRIPTION = role.DESCRIPTION;
            existingRole.IS_SYSTEM_ROLE = role.IS_SYSTEM_ROLE;
            
            // Update audit fields
            existingRole.UPDATED = DateTime.Now;
            existingRole.UPDATEDBY = _user.UserName ?? string.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return existingRole;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.ROLEs.AnyAsync(r => r.GUID == role.GUID && r.DELETED == null))
                {
                    throw new KeyNotFoundException($"Role with ID {role.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(int id, string deletedBy)
        {
            var role = await _context.ROLEs
                .FirstOrDefaultAsync(r => r.GUID == id && r.DELETED == null);

            if (role == null)
                return false;

            // Prevent deletion of system roles
            if (role.IS_SYSTEM_ROLE)
                throw new InvalidOperationException("System roles cannot be deleted");

            role.DELETED = DateTime.Now;
            role.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ROLEs
                .AnyAsync(r => r.GUID == id && r.DELETED == null);
        }
    }
}
