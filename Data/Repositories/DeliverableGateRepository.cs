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
    public class DeliverableGateRepository : IDeliverableGateRepository
    {
        private readonly FourSPMContext _context;

        public DeliverableGateRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DELIVERABLE_GATE>> GetAllAsync()
        {
            return await _context.DELIVERABLE_GATEs
                .Where(dg => dg.DELETED == null)
                .OrderBy(dg => dg.AUTO_PERCENTAGE)
                .ToListAsync();
        }

        public async Task<DELIVERABLE_GATE?> GetByIdAsync(Guid id)
        {
            return await _context.DELIVERABLE_GATEs
                .FirstOrDefaultAsync(dg => dg.GUID == id && dg.DELETED == null);
        }

        public async Task<DELIVERABLE_GATE> CreateAsync(DELIVERABLE_GATE deliverableGate, Guid? createdBy)
        {
            deliverableGate.CREATED = DateTime.Now;
            deliverableGate.CREATEDBY = createdBy ?? Guid.Empty;
            
            _context.DELIVERABLE_GATEs.Add(deliverableGate);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(deliverableGate.GUID) ?? deliverableGate;
        }

        public async Task<DELIVERABLE_GATE> UpdateAsync(DELIVERABLE_GATE deliverableGate, Guid? updatedBy)
        {
            // Update audit fields directly on the passed object
            deliverableGate.UPDATED = DateTime.Now;
            deliverableGate.UPDATEDBY = updatedBy ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return deliverableGate;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.DELIVERABLE_GATEs.AnyAsync(dg => dg.GUID == deliverableGate.GUID && dg.DELETED == null))
                {
                    throw new KeyNotFoundException($"Deliverable gate with ID {deliverableGate.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var deliverableGate = await _context.DELIVERABLE_GATEs
                .FirstOrDefaultAsync(dg => dg.GUID == id && dg.DELETED == null);

            if (deliverableGate == null)
                return false;

            deliverableGate.DELETED = DateTime.Now;
            deliverableGate.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.DELIVERABLE_GATEs
                .AnyAsync(dg => dg.GUID == id && dg.DELETED == null);
        }
    }
}
