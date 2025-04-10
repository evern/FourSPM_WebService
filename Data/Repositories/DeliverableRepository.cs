using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DeliverableRepository>? _logger;

        public DeliverableRepository(FourSPMContext context, ApplicationUser user, ILogger<DeliverableRepository>? logger = null)
        {
            _context = context;
            _user = user;
            _logger = logger;
        }

        public IQueryable<DELIVERABLE> GetAllAsync()
        {
            // Return IQueryable<DELIVERABLE> directly for optimal OData performance
            return _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.ProgressItems)
                .Include(d => d.DeliverableGate)
                .Where(d => d.DELETED == null)
                // Only include Standard deliverables and ApprovedVariation deliverables
                .Where(d => d.VARIATION_STATUS == (int)VariationStatus.Standard || 
                             d.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation)
                .OrderByDescending(d => d.CREATED);
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
                // Only include Standard deliverables and ApprovedVariation deliverables
                .Where(d => d.VARIATION_STATUS == (int)VariationStatus.Standard || 
                             d.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation)
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

        /// <summary>
        /// Gets merged variation deliverables for a given variation and eligible original deliverables that could be added
        /// Uses pure IQueryable approach for optimal OData integration and virtual scrolling support
        /// </summary>
        /// <param name="variationGuid">Optional variation GUID to filter by. If not provided, returns all variation deliverables.</param>
        /// <returns>IQueryable of deliverables including both variation-specific ones and eligible standards</returns>
        public IQueryable<DELIVERABLE> GetMergedVariationDeliverables(Guid? variationGuid = null)
        {
            _logger?.LogInformation($"Getting all variation deliverables with pure IQueryable support. VariationId={variationGuid}");
            
            // Get the base query with all needed includes - this ensures all results have proper navigation properties
            var baseQuery = _context.DELIVERABLEs
                .Include(d => d.Project)
                    .ThenInclude(p => p != null ? p.Client : null!)
                .Include(d => d.DeliverableGate)
                .Include(d => d.ProgressItems)
                .Where(d => d.DELETED == null);

            // If no variation GUID provided, just return the base query
            if (!variationGuid.HasValue || variationGuid.Value == Guid.Empty)
            {
                _logger?.LogInformation("No variation ID provided - returning all variation deliverables");
                return baseQuery.Where(d => d.GUID_VARIATION != null);
            }

            // 1. First define the query for deliverables specific to this variation
            var variationDeliverables = baseQuery.Where(d => d.GUID_VARIATION == variationGuid.Value);

            // 2. Find the project ID for this variation using a subquery rather than materializing
            var projectGuidQuery = _context.VARIATIONs
                .Where(v => v.GUID == variationGuid.Value)
                .Select(v => v.GUID_PROJECT);

            // 3. Get all original deliverable GUIDs already in this variation using a subquery
            var deliverablesInVariationQuery = variationDeliverables
                .Where(d => d.GUID_ORIGINAL_DELIVERABLE != null)
                .Select(d => d.GUID_ORIGINAL_DELIVERABLE);

            // 4. Create the query for eligible standard deliverables (not yet in this variation)
            var eligibleStandardsQuery = baseQuery
                .Where(d => 
                    // Match project with variation's project
                    projectGuidQuery.Contains(d.GUID_PROJECT) && 
                    // Only standard deliverables (not variations or cancellations)
                    d.GUID_VARIATION == null && 
                    // Make sure not already in this variation
                    !deliverablesInVariationQuery.Contains(d.GUID) &&
                    // Ensure it's a standard deliverable
                    d.VARIATION_STATUS == (int)VariationStatus.Standard);

            // 5. UNION the two queryables - this preserves IQueryable nature for OData operations
            var combinedQuery = variationDeliverables.Union(eligibleStandardsQuery);
            
            _logger?.LogInformation("Successfully created combined query with variation and eligible standard deliverables");
            return combinedQuery;
        }

        /// <summary>
        /// Checks if a deliverable from the original scope has already been added to a specific variation
        /// </summary>
        /// <param name="originalDeliverableId">The GUID of the original deliverable to check for</param>
        /// <param name="variationId">The GUID of the variation to search within</param>
        /// <returns>The existing variation deliverable if found, or null if the original deliverable has not been added to this variation</returns>
        public async Task<DELIVERABLE?> FindExistingVariationDeliverableAsync(Guid originalDeliverableId, Guid variationId)
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

        /// <summary>
        /// Creates a copy of a deliverable with the specified properties for variations
        /// </summary>
        private DELIVERABLE CreateDeliverableCopy(DELIVERABLE original, Guid variationId, int variationStatus)
        {
            return new DELIVERABLE
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
                VARIATION_HOURS = 0,
                APPROVED_VARIATION_HOURS = 0,
                BOOKING_CODE = original.BOOKING_CODE,
                TOTAL_COST = original.TOTAL_COST,
                CREATED = DateTime.Now,
                CREATEDBY = _user.UserId ?? Guid.Empty,
                VARIATION_STATUS = variationStatus
            };
        }

        public async Task<DELIVERABLE> CreateVariationCopyAsync(DELIVERABLE original, Guid variationId, int variationStatus)
        {
            // Create a new instance with copied properties from the original
            var variationCopy = CreateDeliverableCopy(original, variationId, variationStatus);

            _context.DELIVERABLEs.Add(variationCopy);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(variationCopy.GUID) ?? variationCopy;
        }

        /// <summary>
        /// Cancels or un-cancels a deliverable based on its current state
        /// </summary>
        /// <param name="originalDeliverableGuid">The original deliverable GUID to cancel or un-cancel</param>
        /// <param name="variationGuid">The variation GUID this operation belongs to</param>
        /// <returns>The updated or newly created deliverable</returns>
        public async Task<DELIVERABLE> CancelDeliverableAsync(Guid originalDeliverableGuid, Guid? variationGuid)
        {
            // Find both the original deliverable and any variation copy for the current variation
            // We need to check both because the user might be attempting to cancel either one
            var deliverables = await _context.DELIVERABLEs
                .Where(d => (d.GUID == originalDeliverableGuid || 
                            (d.GUID_ORIGINAL_DELIVERABLE == originalDeliverableGuid && d.GUID_VARIATION == variationGuid)) && 
                            d.DELETED == null)
                .ToListAsync();
            
            // Prioritize the variation copy if it exists, otherwise use the original
            // This is crucial: if a variation copy exists, we are essentially reverting it to original
            // by marking it as deleted rather than creating a new cancellation record
            var deliverable = deliverables.FirstOrDefault(d => d.GUID_ORIGINAL_DELIVERABLE == originalDeliverableGuid && d.GUID_VARIATION == variationGuid) ?? 
                             deliverables.FirstOrDefault(d => d.GUID == originalDeliverableGuid);
            
            if (deliverable == null)
            {
                throw new KeyNotFoundException($"Deliverable with GUID {originalDeliverableGuid} not found or already deleted");
            }
            
            // Start a transaction for atomic operations
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // CASE 1: Already in an unapproved variation
                if (deliverable.GUID_VARIATION != null && 
                    deliverable.VARIATION_STATUS != (int)VariationStatus.ApprovedVariation)
                {
                    // Check if it's a cancellation - if so, un-cancel it
                    if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation)
                    {
                        // This is an un-cancel operation - mark the cancellation as deleted
                        deliverable.DELETED = DateTime.Now;
                        deliverable.DELETEDBY = _user.UserId ?? Guid.Empty;
                        deliverable.VARIATION_HOURS = 0;  // Reset hours on cancellation
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return deliverable;
                    }
                    else
                    {
                        // For other unapproved variations, just mark as deleted (normal cancellation)
                        deliverable.DELETED = DateTime.Now;
                        deliverable.DELETEDBY = _user.UserId ?? Guid.Empty;
                        
                        // If it's an in-progress variation, reset hours
                        if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedVariation)
                        {
                            deliverable.VARIATION_HOURS = 0;
                        }
                    }
                }
                // CASE 2: Standard deliverable or approved variation
                else
                {
                    // Check if cancellation already exists for this deliverable in the same variation
                    var existingCancellation = await _context.DELIVERABLEs
                        .FirstOrDefaultAsync(d => d.GUID_ORIGINAL_DELIVERABLE == deliverable.GUID && 
                                                  d.GUID_VARIATION == variationGuid &&
                                                  d.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation &&
                                                  d.DELETED == null);
                    
                    if (existingCancellation != null)
                    {
                        // This is an un-cancel operation - mark the cancellation as deleted
                        existingCancellation.DELETED = DateTime.Now;
                        existingCancellation.DELETEDBY = _user.UserId ?? Guid.Empty;
                        existingCancellation.VARIATION_HOURS = 0;  // Reset hours on cancellation
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return existingCancellation;
                    }
                    
                    // Make sure variationGuid has value
                    if (!variationGuid.HasValue)
                    {
                        throw new ArgumentException("Variation GUID is required for creating a cancellation deliverable");
                    }
                    
                    // Create cancellation variation keeping original title
                    var cancellationDeliverable = CreateDeliverableCopy(
                        deliverable, 
                        variationGuid.Value, 
                        (int)VariationStatus.UnapprovedCancellation);
                    
                    _context.DELIVERABLEs.Add(cancellationDeliverable);
                    deliverable = cancellationDeliverable;
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return deliverable;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
