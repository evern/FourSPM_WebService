using FourSPM_WebService.Data.EF.FourSPM;
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

        public DisciplineRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DISCIPLINE>> GetAllAsync()
        {
            return await _context.Set<DISCIPLINE>()
                .Where(d => d.DELETED == null)
                .OrderBy(d => d.CODE)
                .ToListAsync();
        }

        public async Task<DISCIPLINE?> GetByIdAsync(Guid id)
        {
            return await _context.Set<DISCIPLINE>()
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DISCIPLINE?> GetByCodeAsync(string code)
        {
            return await _context.Set<DISCIPLINE>()
                .FirstOrDefaultAsync(d => d.CODE == code && d.DELETED == null);
        }

        public async Task<DISCIPLINE> CreateAsync(DISCIPLINE discipline)
        {
            discipline.CREATED = DateTime.UtcNow;
            
            _context.Set<DISCIPLINE>().Add(discipline);
            await _context.SaveChangesAsync();
            
            return discipline;
        }

        public async Task<DISCIPLINE> UpdateAsync(DISCIPLINE discipline)
        {
            var existingDiscipline = await _context.Set<DISCIPLINE>()
                .FirstOrDefaultAsync(d => d.GUID == discipline.GUID && d.DELETED == null);
            
            if (existingDiscipline == null)
                throw new KeyNotFoundException($"Discipline with ID {discipline.GUID} not found");

            // Update properties
            existingDiscipline.CODE = discipline.CODE;
            existingDiscipline.NAME = discipline.NAME;
            existingDiscipline.UPDATED = DateTime.UtcNow;
            existingDiscipline.UPDATEDBY = discipline.UPDATEDBY;

            await _context.SaveChangesAsync();
            return existingDiscipline;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var discipline = await _context.Set<DISCIPLINE>()
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
            
            if (discipline == null)
                return false;

            discipline.DELETED = DateTime.UtcNow;
            discipline.DELETEDBY = deletedBy;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
