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
                .Include(d => d.ProgressItems)
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

        public async Task<IEnumerable<DELIVERABLE>> GetByProjectIdAndPeriodAsync(Guid projectId, int period)
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

        public async Task<IEnumerable<DELIVERABLE>> GetByVariationIdAsync(Guid variationId)
        {
            // First, get the project GUID associated with this variation
            var variation = await _context.VARIATIONs
                .FirstOrDefaultAsync(v => v.GUID == variationId && v.DELETED == null);
            
            if (variation == null)
            {
                return new List<DELIVERABLE>();
            }
            
            var projectId = variation.GUID_PROJECT;
            
            // Perform a query that returns:
            // 1. Standard deliverables (no variation) that don't have a variation version for this variation
            // 2. Approved variations from any variation
            // 3. Unapproved variations but only from the specified variation ID
            var result = await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.DeliverableGate)
                .Where(d =>
                    d.GUID_PROJECT == projectId &&
                    d.DELETED == null &&
                    (
                        // Standard deliverables with no variation version in this variation
                        (d.VARIATION_STATUS == (int)VariationStatus.Standard &&
                         !_context.DELIVERABLEs.Any(vd => 
                             vd.GUID_VARIATION == variationId && 
                             vd.GUID_ORIGINAL_DELIVERABLE == d.GUID && 
                             vd.DELETED == null)) ||
                        
                        // Approved variations from any variation
                        (d.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation) ||
                        
                        // Unapproved variations only from the specific variation ID
                        (d.VARIATION_STATUS == (int)VariationStatus.UnapprovedVariation && 
                         d.GUID_VARIATION == variationId)
                    )
                )
                .ToListAsync();

            return result;
        }

        public async Task<DELIVERABLE?> GetVariationCopyAsync(Guid originalDeliverableId, Guid variationId)
        {
            return await _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.ProgressItems)
                .Include(d => d.DeliverableGate)
                .FirstOrDefaultAsync(d => d.GUID_ORIGINAL_DELIVERABLE == originalDeliverableId 
                                     && d.GUID_VARIATION == variationId 
                                     && d.DELETED == null);
        }

        public async Task<DELIVERABLE> CreateVariationCopyAsync(DELIVERABLE original, Guid variationId, int variationStatus)
        {
            // Create a new instance with copied properties from the original
            var variationCopy = new DELIVERABLE
            {
                GUID = Guid.NewGuid(),
                GUID_PROJECT = original.GUID_PROJECT,
                GUID_VARIATION = variationId,
                GUID_ORIGINAL_DELIVERABLE = original.GUID,
                AREA_NUMBER = original.AREA_NUMBER,
                DISCIPLINE = original.DISCIPLINE,
                DOCUMENT_TYPE = original.DOCUMENT_TYPE,
                DEPARTMENT_ID = original.DEPARTMENT_ID,
                DELIVERABLE_TYPE_ID = original.DELIVERABLE_TYPE_ID,
                GUID_DELIVERABLE_GATE = original.GUID_DELIVERABLE_GATE,
                INTERNAL_DOCUMENT_NUMBER = original.INTERNAL_DOCUMENT_NUMBER,
                CLIENT_DOCUMENT_NUMBER = original.CLIENT_DOCUMENT_NUMBER,
                DOCUMENT_TITLE = original.DOCUMENT_TITLE,
                BUDGET_HOURS = original.BUDGET_HOURS,
                VARIATION_HOURS = 0, // Start with zero variation hours
                APPROVED_VARIATION_HOURS = 0,
                BOOKING_CODE = original.BOOKING_CODE,
                TOTAL_COST = original.TOTAL_COST,
                CREATED = DateTime.Now,
                CREATEDBY = _user.UserId ?? Guid.Empty,
                VARIATION_STATUS = variationStatus
            };

            _context.DELIVERABLEs.Add(variationCopy);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(variationCopy.GUID) ?? variationCopy;
        }

        public async Task<DELIVERABLE> CreateNewVariationDeliverableAsync(DELIVERABLE deliverable, Guid variationId)
        {
            // Set variation-specific properties
            deliverable.GUID = Guid.NewGuid();
            deliverable.GUID_VARIATION = variationId;
            deliverable.VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation;
            deliverable.CREATED = DateTime.Now;
            deliverable.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _context.DELIVERABLEs.Add(deliverable);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(deliverable.GUID) ?? deliverable;
        }

        public async Task<DELIVERABLE> CreateVariationCancellationAsync(Guid originalDeliverableId, Guid variationId)
        {
            // Find the original deliverable
            var original = await GetByIdAsync(originalDeliverableId);
            if (original == null)
            {
                throw new KeyNotFoundException($"Deliverable with ID {originalDeliverableId} not found");
            }

            // Check if a cancellation copy already exists
            var existingCancellation = await _context.DELIVERABLEs
                .FirstOrDefaultAsync(d => d.GUID_ORIGINAL_DELIVERABLE == originalDeliverableId 
                                   && d.GUID_VARIATION == variationId 
                                   && (d.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation 
                                       || d.VARIATION_STATUS == (int)VariationStatus.ApprovedCancellation) 
                                   && d.DELETED == null);

            if (existingCancellation != null)
            {
                return existingCancellation; // Return existing cancellation if found
            }

            // Create a cancellation copy
            var cancellationCopy = new DELIVERABLE
            {
                GUID = Guid.NewGuid(),
                GUID_PROJECT = original.GUID_PROJECT,
                GUID_VARIATION = variationId,
                GUID_ORIGINAL_DELIVERABLE = original.GUID,
                AREA_NUMBER = original.AREA_NUMBER,
                DISCIPLINE = original.DISCIPLINE,
                DOCUMENT_TYPE = original.DOCUMENT_TYPE,
                DEPARTMENT_ID = original.DEPARTMENT_ID,
                DELIVERABLE_TYPE_ID = original.DELIVERABLE_TYPE_ID,
                GUID_DELIVERABLE_GATE = original.GUID_DELIVERABLE_GATE,
                INTERNAL_DOCUMENT_NUMBER = original.INTERNAL_DOCUMENT_NUMBER,
                CLIENT_DOCUMENT_NUMBER = original.CLIENT_DOCUMENT_NUMBER,
                DOCUMENT_TITLE = original.DOCUMENT_TITLE,
                BUDGET_HOURS = original.BUDGET_HOURS,
                VARIATION_HOURS = -original.BUDGET_HOURS, // Negative hours to cancel out the original
                APPROVED_VARIATION_HOURS = 0,
                BOOKING_CODE = original.BOOKING_CODE,
                TOTAL_COST = original.TOTAL_COST,
                CREATED = DateTime.Now,
                CREATEDBY = _user.UserId ?? Guid.Empty,
                VARIATION_STATUS = (int)VariationStatus.UnapprovedCancellation
            };

            _context.DELIVERABLEs.Add(cancellationCopy);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(cancellationCopy.GUID) ?? cancellationCopy;
        }
    }
}
