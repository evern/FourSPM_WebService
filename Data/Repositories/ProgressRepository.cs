using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly FourSPMContext _context;

        public ProgressRepository(FourSPMContext context)
        {
            _context = context;
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
            _context.PROGRESSes.Add(progress);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(progress.GUID) ?? progress;
        }

        public async Task<PROGRESS> UpdateAsync(PROGRESS progress)
        {
            var existingProgress = await _context.PROGRESSes
                .FirstOrDefaultAsync(p => p.GUID == progress.GUID && p.DELETED == null);

            if (existingProgress == null)
                throw new KeyNotFoundException($"Progress with ID {progress.GUID} not found");

            existingProgress.GUID_DELIVERABLE = progress.GUID_DELIVERABLE;
            existingProgress.PERIOD = progress.PERIOD;
            existingProgress.UNITS = progress.UNITS;
            existingProgress.UPDATED = DateTime.Now;
            existingProgress.UPDATEDBY = progress.UPDATEDBY;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(existingProgress.GUID) ?? existingProgress;
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
    }
}
