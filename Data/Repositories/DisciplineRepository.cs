using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class DisciplineRepository : IDisciplineRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public DisciplineRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<DISCIPLINE>> GetAllAsync()
        {
            return await _context.DISCIPLINEs
                .Where(d => d.DELETED == null)
                .OrderBy(d => d.CODE)
                .ToListAsync();
        }

        public async Task<DISCIPLINE?> GetByIdAsync(Guid id)
        {
            return await _context.DISCIPLINEs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DISCIPLINE?> GetByCodeAsync(string code)
        {
            return await _context.DISCIPLINEs
                .FirstOrDefaultAsync(d => d.CODE == code && d.DELETED == null);
        }

        public async Task<DISCIPLINE> CreateAsync(DISCIPLINE discipline)
        {
            discipline.CREATED = DateTime.Now;
            discipline.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _context.DISCIPLINEs.Add(discipline);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(discipline.GUID) ?? discipline;
        }

        public async Task<DISCIPLINE> UpdateAsync(DISCIPLINE discipline)
        {
            // Update audit fields directly on the passed object
            discipline.UPDATED = DateTime.Now;
            discipline.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return discipline;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.DISCIPLINEs.AnyAsync(d => d.GUID == discipline.GUID && d.DELETED == null))
                {
                    throw new KeyNotFoundException($"Discipline with ID {discipline.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var discipline = await _context.DISCIPLINEs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
            
            if (discipline == null)
                return false;

            discipline.DELETED = DateTime.Now;
            discipline.DELETEDBY = deletedBy;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.DISCIPLINEs
                .AnyAsync(d => d.GUID == id && d.DELETED == null);
        }
    }
}
