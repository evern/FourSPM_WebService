using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class DeliverableRepository : IDeliverableRepository
    {
        private readonly FourSPMContext _context;

        public DeliverableRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DELIVERABLE>> GetAllAsync()
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Where(d => d.DELETED == null)
                .OrderByDescending(d => d.CREATED)
                .ToListAsync();
        }

        public async Task<IEnumerable<DELIVERABLE>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Where(d => d.PROJECT_GUID == projectId && d.DELETED == null)
                .ToListAsync();
        }

        public async Task<DELIVERABLE?> GetByIdAsync(Guid id)
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DELIVERABLE> CreateAsync(DELIVERABLE deliverable)
        {
            deliverable.CREATED = DateTime.Now;
            _context.DELIVERABLEs.Add(deliverable);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(deliverable.GUID) ?? deliverable;
        }

        public async Task<DELIVERABLE> UpdateAsync(DELIVERABLE deliverable)
        {
            var existingDeliverable = await _context.DELIVERABLEs
                .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID && d.DELETED == null);

            if (existingDeliverable == null)
                throw new KeyNotFoundException($"Deliverable with ID {deliverable.GUID} not found");

            existingDeliverable.AREA_NUMBER = deliverable.AREA_NUMBER;
            existingDeliverable.DISCIPLINE = deliverable.DISCIPLINE;
            existingDeliverable.DOCUMENT_TYPE = deliverable.DOCUMENT_TYPE;
            existingDeliverable.DEPARTMENT_ID = deliverable.DEPARTMENT_ID;
            existingDeliverable.DELIVERABLE_TYPE_ID = deliverable.DELIVERABLE_TYPE_ID;
            existingDeliverable.INTERNAL_DOCUMENT_NUMBER = deliverable.INTERNAL_DOCUMENT_NUMBER;
            existingDeliverable.CLIENT_DOCUMENT_NUMBER = deliverable.CLIENT_DOCUMENT_NUMBER;
            existingDeliverable.DOCUMENT_TITLE = deliverable.DOCUMENT_TITLE;
            existingDeliverable.BUDGET_HOURS = deliverable.BUDGET_HOURS;
            existingDeliverable.VARIATION_HOURS = deliverable.VARIATION_HOURS;
            existingDeliverable.TOTAL_COST = deliverable.TOTAL_COST;
            existingDeliverable.UPDATED = DateTime.Now;
            existingDeliverable.UPDATEDBY = deliverable.UPDATEDBY;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(existingDeliverable.GUID) ?? existingDeliverable;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var deliverable = await _context.DELIVERABLEs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);

            if (deliverable == null)
                return false;

            deliverable.DELETED = DateTime.Now;
            deliverable.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.DELIVERABLEs
                .AnyAsync(d => d.GUID == id && d.DELETED == null);
        }
    }
}
