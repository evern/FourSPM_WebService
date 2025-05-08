using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Utilities;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class VariationRepository : IVariationRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public VariationRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<VARIATION>> GetAllAsync()
        {
            return await _context.VARIATIONs
                .Where(v => v.DELETED == null)
                .Include(v => v.Project)
                .OrderByDescending(v => v.CREATED)
                .ToListAsync();
        }

        public async Task<VARIATION?> GetByIdAsync(Guid id)
        {
            return await _context.VARIATIONs
                .Where(v => v.GUID == id && v.DELETED == null)
                .Include(v => v.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<VARIATION>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.VARIATIONs
                .Where(v => v.GUID_PROJECT == projectId && v.DELETED == null)
                .Include(v => v.Project)
                .OrderByDescending(v => v.CREATED)
                .ToListAsync();
        }

        public async Task<VARIATION> CreateAsync(VARIATION variation)
        {
            variation.GUID = Guid.NewGuid();
            variation.CREATED = DateTime.Now;
            variation.CREATEDBY = _user.UserId ?? Guid.Empty;

            // Update date/user fields
            SyncDateAndUserFields(variation);

            _context.VARIATIONs.Add(variation);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(variation.GUID) ?? variation;
        }

        public async Task<VARIATION> UpdateAsync(VARIATION variation)
        {
            // Update audit fields directly on the passed object
            variation.UPDATED = DateTime.Now;
            variation.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return variation;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.VARIATIONs.AnyAsync(v => v.GUID == variation.GUID && v.DELETED == null))
                {
                    throw new KeyNotFoundException($"Variation with ID {variation.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            // Check if the variation exists and isn't already deleted
            if (!await _context.VARIATIONs.AnyAsync(v => v.GUID == id && v.DELETED == null))
                return false;

            // Use ExecuteUpdateAsync instead of tracked entities to avoid OUTPUT clause issues with triggers
            // This directly generates a SQL UPDATE command without the OUTPUT clause
            var affectedRows = await _context.VARIATIONs
                .Where(v => v.GUID == id && v.DELETED == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.DELETED, DateTime.Now)
                    .SetProperty(v => v.DELETEDBY, deletedBy)
                );

            return affectedRows > 0;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.VARIATIONs
                .AnyAsync(v => v.GUID == id && v.DELETED == null);
        }

        /// <summary>
        /// Synchronizes date fields with their corresponding user fields
        /// </summary>
        /// <param name="variation">The variation to update</param>
        private void SyncDateAndUserFields(VARIATION variation)
        {
            // Handle SUBMITTED date and user association
            if (variation.SUBMITTED.HasValue)
            {
                // If SUBMITTED date exists but SUBMITTEDBY is not set or empty, set it to current user
                if (!variation.SUBMITTEDBY.HasValue || variation.SUBMITTEDBY == Guid.Empty)
                {
                    variation.SUBMITTEDBY = _user.UserId ?? Guid.Empty;
                }
            }
            else
            {
                // If SUBMITTED date is null, also clear the SUBMITTEDBY field
                variation.SUBMITTEDBY = null;
            }

            // Handle CLIENT_APPROVED date and user association
            if (variation.CLIENT_APPROVED.HasValue)
            {
                // If CLIENT_APPROVED date exists but CLIENT_APPROVEDBY is not set or empty, set it to current user
                if (!variation.CLIENT_APPROVEDBY.HasValue || variation.CLIENT_APPROVEDBY == Guid.Empty)
                {
                    variation.CLIENT_APPROVEDBY = _user.UserId ?? Guid.Empty;
                }
            }
            else
            {
                // If CLIENT_APPROVED date is null, also clear the CLIENT_APPROVEDBY field
                variation.CLIENT_APPROVEDBY = null;
            }
        }

        public async Task<VARIATION> ApproveVariationAsync(Guid variationGuid)
        {
            // Find the variation to approve
            var variation = await _context.VARIATIONs
                .Where(v => v.GUID == variationGuid && v.DELETED == null)
                .FirstOrDefaultAsync();
            
            if (variation == null)
            {
                throw new KeyNotFoundException($"Variation with ID {variationGuid} not found");
            }
            
            // Check if already approved - prevents duplicate approvals
            if (variation.CLIENT_APPROVED.HasValue)
            {
                throw new InvalidOperationException("This variation has already been approved");
            }
            
            // Check if variation has been submitted - cannot approve unsubmitted variations
            if (!variation.SUBMITTED.HasValue)
            {
                throw new InvalidOperationException("This variation must be submitted before it can be approved");
            }

            // Start a transaction to ensure all updates are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            // Update variation approval fields immediately to minimize concurrency window
            variation.CLIENT_APPROVED = DateTime.Now;
            variation.CLIENT_APPROVEDBY = _user.UserId ?? Guid.Empty;
            variation.UPDATED = DateTime.Now;
            variation.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                // Get all deliverables related to this variation
                var deliverables = await _context.DELIVERABLEs
                    .Where(d => d.GUID_VARIATION == variationGuid && d.DELETED == null)
                    .ToListAsync();

                // Process each deliverable to update its status
                foreach (var deliverable in deliverables)
                {
                    // Update the appropriate fields based on deliverable type
                    if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedVariation)
                    {
                        // For variations: Update status to approved
                        deliverable.VARIATION_STATUS = (int)VariationStatus.ApprovedVariation;
                        
                        // Check if this is a self-referencing original deliverable in a variation
                        // This means it's the original deliverable that was directly added to the variation
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue && 
                            deliverable.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE.Value && 
                            deliverable.GUID_VARIATION == variationGuid)
                        {
                            // Update the internal document number by replacing the XXX suffix with a proper sequence number
                            if (!string.IsNullOrEmpty(deliverable.INTERNAL_DOCUMENT_NUMBER) &&
                                deliverable.INTERNAL_DOCUMENT_NUMBER.EndsWith("-XXX"))
                            {
                                // Get the base format (everything before -XXX)
                                string baseFormat = DocumentNumberGenerator.GetBaseFormatFromXXX(deliverable.INTERNAL_DOCUMENT_NUMBER);
                                    
                                // Generate a new sequence number using the shared utility
                                string sequenceNumber = await DocumentNumberGenerator.GenerateSequenceNumberAsync(
                                    _context, baseFormat, deliverable.GUID_PROJECT, deliverable.GUID);
                                
                                // Update the document number
                                deliverable.INTERNAL_DOCUMENT_NUMBER = $"{baseFormat}-{sequenceNumber}";
                            }
                        }
                        
                        // For variations: Add the variation hours to approved hours
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
                        {
                            // Find original deliverable to update its approved variation hours
                            var originalDeliverable = await _context.DELIVERABLEs
                                .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                                
                            if (originalDeliverable != null)
                            {
                                // Update the original deliverable's approved variation hours
                                originalDeliverable.APPROVED_VARIATION_HOURS = 
                                    originalDeliverable.APPROVED_VARIATION_HOURS + deliverable.VARIATION_HOURS;
                                    
                                originalDeliverable.UPDATED = DateTime.Now;
                                originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                            }
                        }
                    }
                    else if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation)
                    {
                        // For cancellations: Update status to approved
                        deliverable.VARIATION_STATUS = (int)VariationStatus.ApprovedCancellation;
                        
                        // For cancellations: Check if there are any progress hours to account for
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
                        {
                            // Find original deliverable to update its approved variation hours
                            var originalDeliverable = await _context.DELIVERABLEs
                                .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                                
                            if (originalDeliverable != null)
                            {
                                // Check if there's a PROGRESS_HOURS property or if we need to sum from progress items
                                decimal progressHours = 0;
                                
                                // Sum progress hours from related progress items if needed
                                var progressItems = await _context.PROGRESSes
                                    .Where(p => p.GUID_DELIVERABLE == originalDeliverable.GUID && p.DELETED == null)
                                    .ToListAsync();
                                    
                                if (progressItems != null && progressItems.Any())
                                {
                                    progressHours = progressItems.Sum(p => p.UNITS);
                                }
                                
                                // Calculate total hours (budget + approved variation hours)
                                decimal totalHours = originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS;
                                
                                // Calculate what the approved hours should be based on actual progress vs total hours
                                // Allow for negative values to represent underutilization
                                decimal calculatedHours = progressHours - totalHours;
                                
                                // Calculate the delta between total hours and calculated hours
                                // This represents how much this variation contributes to the overall allocation
                                decimal delta = totalHours - progressHours;
                                
                                // Store the delta in the variation's APPROVED_VARIATION_HOURS field for tracking purposes
                                // This is clearer when viewing variation data than using VARIATION_HOURS
                                deliverable.APPROVED_VARIATION_HOURS = delta;
                                
                                // Update the approved variation hours to match actual progress vs budget
                                // Use += to preserve variation hours from other variations
                                originalDeliverable.APPROVED_VARIATION_HOURS += calculatedHours;
                                originalDeliverable.UPDATED = DateTime.Now;
                                originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                            }
                        }
                    }
                    
                    // Common updates for all deliverable types
                    deliverable.UPDATED = DateTime.Now;
                    deliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                }

                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return variation;
            }
            catch (Exception)
            {
                // Rollback the transaction on any error
                await transaction.RollbackAsync();
                
                // Reset approval fields since the operation failed
                variation.CLIENT_APPROVED = null;
                variation.CLIENT_APPROVEDBY = null;
                
                throw;
            }
        }

        public async Task<VARIATION> RejectVariationAsync(Guid variationGuid)
        {
            // Find the variation to reject
            var variation = await _context.VARIATIONs
                .Where(v => v.GUID == variationGuid && v.DELETED == null)
                .FirstOrDefaultAsync();
            
            if (variation == null)
            {
                throw new KeyNotFoundException($"Variation with ID {variationGuid} not found");
            }
            
            // Check if variation is approved - can only reject approved variations
            if (!variation.CLIENT_APPROVED.HasValue)
            {
                throw new InvalidOperationException("This variation has not been approved yet and cannot be rejected");
            }
            
            // Start a transaction to ensure all updates are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            // Store the original approved values in case we need to restore them
            DateTime? originalApprovedDate = variation.CLIENT_APPROVED;
            Guid? originalApprovedBy = variation.CLIENT_APPROVEDBY;
            
            // Clear variation approval fields
            variation.CLIENT_APPROVED = null;
            variation.CLIENT_APPROVEDBY = null;
            variation.UPDATED = DateTime.Now;
            variation.UPDATEDBY = _user.UserId ?? Guid.Empty;

            try
            {
                // Get all deliverables related to this variation
                var deliverables = await _context.DELIVERABLEs
                    .Where(d => d.GUID_VARIATION == variationGuid && d.DELETED == null)
                    .ToListAsync();

                // Process each deliverable to update its status
                foreach (var deliverable in deliverables)
                {
                    // Update the appropriate fields based on deliverable type
                    if (deliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation)
                    {
                        // For variations: Revert status to unapproved
                        deliverable.VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation;

                        // Check if this is a self-referencing original deliverable in a variation
                        // This means it's the original deliverable that was directly added to the variation
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue && 
                            deliverable.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE.Value && 
                            deliverable.GUID_VARIATION == variationGuid)
                        {
                            // Update the internal document number by replacing the numerical suffix with XXX
                            if (!string.IsNullOrEmpty(deliverable.INTERNAL_DOCUMENT_NUMBER))
                            {
                                // Use the shared utility to replace the numeric suffix with XXX
                                deliverable.INTERNAL_DOCUMENT_NUMBER = 
                                    DocumentNumberGenerator.ReplaceWithXXXSuffix(deliverable.INTERNAL_DOCUMENT_NUMBER);
                            }
                        }
            
                        // Reset approved variation hours on original deliverable
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
                        {
                            // Find original deliverable to update its approved variation hours
                            var originalDeliverable = await _context.DELIVERABLEs
                                .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                                
                            if (originalDeliverable != null)
                            {
                                // Subtract the hours that were previously added
                                decimal currentApprovedHours = originalDeliverable.APPROVED_VARIATION_HOURS;
                                decimal variationHours = deliverable.VARIATION_HOURS;
                                
                                // Prevent negative approved variation hours
                                originalDeliverable.APPROVED_VARIATION_HOURS = Math.Max(0, currentApprovedHours - variationHours);
                                    
                                originalDeliverable.UPDATED = DateTime.Now;
                                originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                            }
                        }
                    }
                    else if (deliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedCancellation)
                    {
                        // For cancellations: Revert status to unapproved
                        deliverable.VARIATION_STATUS = (int)VariationStatus.UnapprovedCancellation;
                    
                        // Reset approved variation hours for cancellations
                        if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
                        {
                            // Find original deliverable to reset its approved variation hours
                            var originalDeliverable = await _context.DELIVERABLEs
                                .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                                
                            if (originalDeliverable != null)
                            {
                                // Restore the tracked delta from the variation to the original deliverable
                                // This undoes the effect that the approved cancellation had on the original
                                originalDeliverable.APPROVED_VARIATION_HOURS += deliverable.APPROVED_VARIATION_HOURS;
                                
                                // Clear the delta from the variation to prevent potential reuse
                                deliverable.APPROVED_VARIATION_HOURS = 0;
                                    
                                originalDeliverable.UPDATED = DateTime.Now;
                                originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                            }
                        }
                    }
                    
                    // Common updates for all deliverable types
                    deliverable.UPDATED = DateTime.Now;
                    deliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
                }

                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return variation;
            }
            catch (Exception)
            {
                // Rollback the transaction on any error
                await transaction.RollbackAsync();
                
                // Restore original approval fields since the operation failed
                variation.CLIENT_APPROVED = originalApprovedDate;
                variation.CLIENT_APPROVEDBY = originalApprovedBy;
                
                throw;
            }
        }
    }
}
