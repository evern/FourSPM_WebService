using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public ProgressRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<PROGRESS>> GetAllAsync()
        {
            return await _context.PROGRESSes
                .Include(p => p.Deliverable)
                .Where(p => p.DELETED == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<PROGRESS>> GetByDeliverableIdAsync(Guid deliverableId)
        {
            return await _context.PROGRESSes
                .Include(p => p.Deliverable)
                .Where(p => p.GUID_DELIVERABLE == deliverableId && p.DELETED == null)
                .ToListAsync();
        }

        public async Task<PROGRESS?> GetByIdAsync(Guid id)
        {
            return await _context.PROGRESSes
                .Include(p => p.Deliverable)
                .FirstOrDefaultAsync(p => p.GUID == id && p.DELETED == null);
        }

        public async Task<PROGRESS> CreateAsync(PROGRESS progress)
        {
            progress.CREATED = DateTime.Now;
            progress.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _context.PROGRESSes.Add(progress);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(progress.GUID) ?? progress;
        }

        public async Task<PROGRESS> UpdateAsync(PROGRESS progress)
        {
            // Update audit fields directly on the passed object
            progress.UPDATED = DateTime.Now;
            progress.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return progress;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.PROGRESSes.AnyAsync(p => p.GUID == progress.GUID && p.DELETED == null))
                {
                    throw new KeyNotFoundException($"Progress with ID {progress.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var progress = await _context.PROGRESSes
                .FirstOrDefaultAsync(p => p.GUID == id && p.DELETED == null);

            if (progress == null)
                return false;

            progress.DELETED = DateTime.Now;
            progress.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.PROGRESSes
                .AnyAsync(p => p.GUID == id && p.DELETED == null);
        }
        
        
        public async Task<bool> HasProgressUnitsAsync(Guid deliverableGuid)
        {
            // Calculate the sum of all UNITS for the deliverable's non-deleted progress records
            var totalUnits = await _context.PROGRESSes
                .Where(p => p.GUID_DELIVERABLE == deliverableGuid && p.DELETED == null)
                .SumAsync(p => p.UNITS);
                
            // Return true if the sum is greater than 0 (deliverable has been progressed)
            return totalUnits > 0;
        }
    }
}
