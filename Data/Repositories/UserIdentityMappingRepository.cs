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
    public class UserIdentityMappingRepository : IUserIdentityMappingRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public UserIdentityMappingRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<USER_IDENTITY_MAPPING>> GetAllAsync()
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .Where(u => u.DELETED == null)
                .OrderByDescending(u => u.CREATED)
                .ToListAsync();
        }

        public async Task<USER_IDENTITY_MAPPING?> GetByIdAsync(Guid id)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.GUID == id && u.DELETED == null);
        }

        public async Task<USER_IDENTITY_MAPPING?> GetByEmailAsync(string email)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.EMAIL == email && u.DELETED == null);
        }

        public async Task<USER_IDENTITY_MAPPING?> GetByUsernameAsync(string username)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.USERNAME == username && u.DELETED == null);
        }

        public async Task<USER_IDENTITY_MAPPING> CreateAsync(USER_IDENTITY_MAPPING userIdentityMapping)
        {
            // Ensure GUID is set
            if (userIdentityMapping.GUID == Guid.Empty)
            {
                userIdentityMapping.GUID = Guid.NewGuid();
            }

            // Set created timestamp
            userIdentityMapping.CREATED = DateTime.Now;
            
            // No need to set CREATEDBY since this is used during authentication
            // before a user is fully established

            _context.USER_IDENTITY_MAPPINGs.Add(userIdentityMapping);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(userIdentityMapping.GUID) ?? userIdentityMapping;
        }

        public async Task<USER_IDENTITY_MAPPING> UpdateAsync(USER_IDENTITY_MAPPING userIdentityMapping)
        {
            var existingMapping = await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.GUID == userIdentityMapping.GUID && u.DELETED == null);

            if (existingMapping == null)
                return null!;

            // Update properties
            existingMapping.USERNAME = userIdentityMapping.USERNAME;
            existingMapping.EMAIL = userIdentityMapping.EMAIL;
            
            try
            {
                await _context.SaveChangesAsync();
                return existingMapping;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.USER_IDENTITY_MAPPINGs.AnyAsync(u => u.GUID == userIdentityMapping.GUID && u.DELETED == null))
                {
                    throw new KeyNotFoundException($"User identity mapping with ID {userIdentityMapping.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var mapping = await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.GUID == id && u.DELETED == null);

            if (mapping == null)
                return false;

            mapping.DELETED = DateTime.Now;
            mapping.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .AnyAsync(u => u.GUID == id && u.DELETED == null);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .AnyAsync(u => u.EMAIL == email && u.DELETED == null);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _context.USER_IDENTITY_MAPPINGs
                .AnyAsync(u => u.USERNAME == username && u.DELETED == null);
        }

        public async Task UpdateLastLoginAsync(Guid id)
        {
            var mapping = await _context.USER_IDENTITY_MAPPINGs
                .FirstOrDefaultAsync(u => u.GUID == id && u.DELETED == null);

            if (mapping != null)
            {
                mapping.LAST_LOGIN = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}
