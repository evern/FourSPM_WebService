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
    public class DeliverableRepository : IDeliverableRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public DeliverableRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<DELIVERABLE>> GetAllAsync()
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(p => p.ProgressItems)
                .Include(d => d.DeliverableGate)
                .Where(d => d.DELETED == null)
                .OrderByDescending(d => d.CREATED)
                .ToListAsync();
        }

        public async Task<IEnumerable<DELIVERABLE>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.ProgressItems)
                .Include(d => d.DeliverableGate)
                .Where(d => d.GUID_PROJECT == projectId && d.DELETED == null)
                .ToListAsync();
        }

        public async Task<DELIVERABLE?> GetByIdAsync(Guid id)
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.ProgressItems)
                .Include(d => d.DeliverableGate)
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DELIVERABLE> CreateAsync(DELIVERABLE deliverable)
        {
            deliverable.CREATED = DateTime.Now;
            deliverable.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _context.DELIVERABLEs.Add(deliverable);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(deliverable.GUID) ?? deliverable;
        }

        public async Task<DELIVERABLE> UpdateAsync(DELIVERABLE deliverable)
        {
            // Update audit fields directly on the passed object
            deliverable.UPDATED = DateTime.Now;
            deliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return deliverable;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.DELIVERABLEs.AnyAsync(d => d.GUID == deliverable.GUID && d.DELETED == null))
                {
                    throw new KeyNotFoundException($"Deliverable with ID {deliverable.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
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

        public async Task<IEnumerable<DELIVERABLE>> GetDeliverablesByNumberPatternAsync(Guid projectId, string pattern)
        {
            return await _context.DELIVERABLEs
                .Where(d => d.GUID_PROJECT == projectId && 
                       d.DELETED == null && 
                       d.INTERNAL_DOCUMENT_NUMBER != null && 
                       d.INTERNAL_DOCUMENT_NUMBER.StartsWith(pattern))
                .ToListAsync();
        }
    }
}
