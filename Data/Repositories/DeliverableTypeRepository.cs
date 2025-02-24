using FourSPM_WebService.Data.EF.FourSPM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class DeliverableTypeRepository : IDeliverableTypeRepository
    {
        private readonly FourSPMContext _context;

        public DeliverableTypeRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DELIVERABLE_TYPE>> GetAllAsync()
        {
            return await _context.DELIVERABLE_TYPEs
                .Where(dt => dt.DELETED == null)
                .ToListAsync();
        }

        public async Task<DELIVERABLE_TYPE?> GetByIdAsync(Guid id)
        {
            return await _context.DELIVERABLE_TYPEs
                .FirstOrDefaultAsync(dt => dt.GUID == id && dt.DELETED == null);
        }

        public async Task<DELIVERABLE_TYPE> CreateAsync(DELIVERABLE_TYPE deliverableType)
        {
            deliverableType.CREATED = DateTime.Now;
            _context.DELIVERABLE_TYPEs.Add(deliverableType);
            await _context.SaveChangesAsync();
            return deliverableType;
        }

        public async Task<DELIVERABLE_TYPE> UpdateAsync(DELIVERABLE_TYPE deliverableType)
        {
            var existingType = await _context.DELIVERABLE_TYPEs
                .FirstOrDefaultAsync(dt => dt.GUID == deliverableType.GUID && dt.DELETED == null);

            if (existingType == null)
                throw new KeyNotFoundException($"DeliverableType with ID {deliverableType.GUID} not found");

            existingType.NAME = deliverableType.NAME;
            existingType.DESCRIPTION = deliverableType.DESCRIPTION;
            existingType.UPDATED = DateTime.Now;
            existingType.UPDATEDBY = deliverableType.UPDATEDBY;

            await _context.SaveChangesAsync();
            return existingType;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var deliverableType = await _context.DELIVERABLE_TYPEs
                .FirstOrDefaultAsync(dt => dt.GUID == id && dt.DELETED == null);

            if (deliverableType == null)
                return false;

            deliverableType.DELETED = DateTime.Now;
            deliverableType.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
