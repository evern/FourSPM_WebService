using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly FourSPMContext _context;

        public DepartmentRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DEPARTMENT>> GetAllAsync()
        {
            return await _context.DEPARTMENTs
                .Where(d => d.DELETED == null)
                .ToListAsync();
        }

        public async Task<DEPARTMENT?> GetByIdAsync(Guid id)
        {
            return await _context.DEPARTMENTs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DEPARTMENT> CreateAsync(DEPARTMENT department)
        {
            department.CREATED = DateTime.Now;
            _context.DEPARTMENTs.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<DEPARTMENT> UpdateAsync(DEPARTMENT department)
        {
            var existingDepartment = await _context.DEPARTMENTs
                .FirstOrDefaultAsync(d => d.GUID == department.GUID && d.DELETED == null);

            if (existingDepartment == null)
                throw new KeyNotFoundException($"Department with ID {department.GUID} not found");

            existingDepartment.NAME = department.NAME;
            existingDepartment.DESCRIPTION = department.DESCRIPTION;
            existingDepartment.UPDATED = DateTime.Now;
            existingDepartment.UPDATEDBY = department.UPDATEDBY;

            await _context.SaveChangesAsync();
            return existingDepartment;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var department = await _context.DEPARTMENTs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);

            if (department == null)
                return false;

            department.DELETED = DateTime.Now;
            department.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
