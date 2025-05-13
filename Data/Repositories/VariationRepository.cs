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

        // Helper method to find and validate a variation for approval
        private async Task<VARIATION> GetVariationForApprovalAsync(Guid variationGuid)
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
            
            return variation;
        }
        
        // Helper method to validate that a variation doesn't depend on unapproved variations
        private async Task ValidateVariationDependenciesAsync(Guid variationGuid)
        {
            // Get deliverables to check for dependencies
            var deliverablesToCheck = await _context.DELIVERABLEs
                .Where(d => d.GUID_VARIATION == variationGuid && d.DELETED == null)
                .ToListAsync();
            
            // Dictionary to track unapproved variations this variation depends on
            Dictionary<Guid, string> unapprovedDependencies = new Dictionary<Guid, string>();
            
            foreach (var deliverable in deliverablesToCheck)
            {
                // Skip deliverables that originated in this variation
                if (deliverable.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE)
                    continue;
                
                // For deliverables that stack hours on others, find the originating variation
                if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
                {
                    // Find the original deliverable
                    var originalDeliverable = await _context.DELIVERABLEs
                        .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE.Value && d.DELETED == null);
                        
                    if (originalDeliverable != null && originalDeliverable.GUID_VARIATION.HasValue && 
                        originalDeliverable.GUID_VARIATION.Value != variationGuid) // Not from current variation
                    {
                        // If the original is from another variation (not a project deliverable),
                        // check if that variation is approved
                        var originatingVariation = await _context.VARIATIONs
                            .FirstOrDefaultAsync(v => v.GUID == originalDeliverable.GUID_VARIATION.Value);
                            
                        if (originatingVariation != null && originatingVariation.CLIENT_APPROVED == null)
                        {
                            // If not approved, add to our dependencies list
                            if (!unapprovedDependencies.ContainsKey(originatingVariation.GUID))
                            {
                                unapprovedDependencies.Add(originatingVariation.GUID, originatingVariation.NAME ?? "Unknown");
                            }
                        }
                    }
                }
            }
            
            // If we found dependencies on unapproved variations, prevent approval
            if (unapprovedDependencies.Any())
            {
                // Build a clean, user-friendly error message with just the dependent variation numbers/names
                var variationsList = string.Join(", ", unapprovedDependencies.Select(v => $"Variation {v.Value}"));
                
                throw new InvalidOperationException(
                    $"Cannot approve this variation because it depends on unapproved variations: {variationsList}");
            }
        }
        
        // Helper method to update document numbers for new deliverables
        private async Task UpdateDeliverableDocumentNumberAsync(DELIVERABLE deliverable, Guid variationGuid)
        {
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
        }
        
        // Helper method to process standard variation deliverable approval
        private async Task ProcessVariationDeliverableApprovalAsync(DELIVERABLE deliverable, Guid variationGuid)
        {
            // Update status to approved
            deliverable.VARIATION_STATUS = (int)VariationStatus.ApprovedVariation;
            
            // Update document numbers for self-referencing deliverables
            await UpdateDeliverableDocumentNumberAsync(deliverable, variationGuid);
            
            // Process variation hours for all deliverables with original references
            if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
            {
                // Find original deliverable to update its approved variation hours
                var originalDeliverable = await _context.DELIVERABLEs
                    .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                    
                if (originalDeliverable != null)
                {
                    await UpdateApprovedVariationHoursAsync(deliverable, originalDeliverable, variationGuid);
                }
            }
        }
        
        // Helper method to calculate and update approved variation hours
        private Task UpdateApprovedVariationHoursAsync(DELIVERABLE deliverable, DELIVERABLE originalDeliverable, Guid variationGuid)
        {
            decimal rate;
            
            // Check if this is a self-referencing deliverable (added directly through variation)
            bool isSelfReferencing = deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue && 
                                    deliverable.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE.Value && 
                                    deliverable.GUID_VARIATION == variationGuid;
                                    
            if (isSelfReferencing)
            {
                // Calculate rate for self-referencing deliverable
                rate = CalculateRateForSelfReferencingDeliverable(deliverable);
            }
            else
            {
                // Calculate rate from original deliverable
                decimal totalHours = originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS;
                rate = totalHours > 0 ? originalDeliverable.TOTAL_COST / totalHours : 0;
            }
            
            // Update the original deliverable's approved variation hours
            originalDeliverable.APPROVED_VARIATION_HOURS = 
                originalDeliverable.APPROVED_VARIATION_HOURS + deliverable.VARIATION_HOURS;
            
            // Update total cost based on the rate and new total hours
            originalDeliverable.TOTAL_COST = (originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS) * rate;
                
            originalDeliverable.UPDATED = DateTime.Now;
            originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            // Return a completed task since this method doesn't need to be async
            return Task.CompletedTask;
        }
        
        // Helper method to calculate rate for a self-referencing deliverable
        private decimal CalculateRateForSelfReferencingDeliverable(DELIVERABLE deliverable)
        {
            if (deliverable.VARIATION_HOURS > 0)
            {
                // Try to use the deliverable's own TOTAL_COST if available
                if (deliverable.TOTAL_COST > 0)
                {
                    return deliverable.TOTAL_COST / deliverable.VARIATION_HOURS;
                }
            }
            
            // Default to 0 when we can't calculate a meaningful rate
            return 0;
        }
        
        // Helper method to process cancellation deliverable approval
        private async Task ProcessCancellationDeliverableApprovalAsync(DELIVERABLE deliverable)
        {
            // Update status to approved
            deliverable.VARIATION_STATUS = (int)VariationStatus.ApprovedCancellation;
            
            // Process cancellation adjustment for deliverables with original references
            if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
            {
                // Find original deliverable to update its approved variation hours
                var originalDeliverable = await _context.DELIVERABLEs
                    .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                    
                if (originalDeliverable != null)
                {
                    await AdjustHoursForCancellationAsync(deliverable, originalDeliverable);
                }
            }
        }
        
        // Helper method to adjust hours for a cancellation
        private async Task AdjustHoursForCancellationAsync(DELIVERABLE deliverable, DELIVERABLE originalDeliverable)
        {
            // Calculate progress hours from related progress items
            decimal progressHours = await CalculateProgressHoursAsync(originalDeliverable);
            
            // Calculate total hours (budget + approved variation hours)
            decimal totalHours = originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS;
            
            // Calculate the current rate (cost per hour) before making any changes
            decimal rate = totalHours > 0 ? originalDeliverable.TOTAL_COST / totalHours : 0;
            
            // Calculate what the approved hours should be based on actual progress vs total hours
            decimal calculatedHours = progressHours - totalHours;
            
            // Calculate the delta between total hours and calculated hours
            decimal deltaHours = totalHours - progressHours;
            
            // Store the delta in the variation's APPROVED_VARIATION_HOURS field for tracking
            deliverable.APPROVED_VARIATION_HOURS = deltaHours;
            
            // Update the approved variation hours and preserve variation hours from other variations
            originalDeliverable.APPROVED_VARIATION_HOURS += calculatedHours;
            
            // Update total cost based on the rate and new total hours
            originalDeliverable.TOTAL_COST = (originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS) * rate;
            
            originalDeliverable.UPDATED = DateTime.Now;
            originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
        }
        
        // Helper method to calculate progress hours for a deliverable
        private async Task<decimal> CalculateProgressHoursAsync(DELIVERABLE deliverable)
        {
            decimal progressHours = 0;
            
            // Sum progress hours from related progress items if needed
            var progressItems = await _context.PROGRESSes
                .Where(p => p.GUID_DELIVERABLE == deliverable.GUID && p.DELETED == null)
                .ToListAsync();
                
            if (progressItems != null && progressItems.Any())
            {
                progressHours = progressItems.Sum(p => p.UNITS);
            }
            
            return progressHours;
        }
        
        // Main method to approve a variation and its deliverables
        public async Task<VARIATION> ApproveVariationAsync(Guid variationGuid)
        {
            // Validate and get the variation to approve
            var variation = await GetVariationForApprovalAsync(variationGuid);
            
            // Check for dependencies on unapproved variations
            await ValidateVariationDependenciesAsync(variationGuid);

            // Start a transaction to ensure all updates are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Mark the variation as approved
                variation.CLIENT_APPROVED = DateTime.Now;
                variation.CLIENT_APPROVEDBY = _user.UserId ?? Guid.Empty;
                variation.UPDATED = DateTime.Now;
                variation.UPDATEDBY = _user.UserId ?? Guid.Empty;
                
                // Get all deliverables related to this variation
                var deliverables = await _context.DELIVERABLEs
                    .Where(d => d.GUID_VARIATION == variationGuid && d.DELETED == null)
                    .ToListAsync();

                // Process each deliverable based on its type
                foreach (var deliverable in deliverables)
                {
                    if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedVariation)
                    {
                        await ProcessVariationDeliverableApprovalAsync(deliverable, variationGuid);
                    }
                    else if (deliverable.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation)
                    {
                        await ProcessCancellationDeliverableApprovalAsync(deliverable);
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

        // Helper method to find and validate a variation for rejection
        private async Task<VARIATION> GetVariationForRejectionAsync(Guid variationGuid)
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
            
            return variation;
        }
        
        // Helper method to check if a variation's deliverables are referenced by other variations
        private async Task ValidateVariationDependentsAsync(Guid variationGuid)
        {
            // Get all deliverables that were introduced in this variation (self-referencing deliverables)
            var originatingDeliverables = await _context.DELIVERABLEs
                .Where(d => d.GUID_VARIATION == variationGuid && 
                          d.GUID_ORIGINAL_DELIVERABLE == d.GUID && // Self-referencing = originated in this variation
                          d.DELETED == null)
                .ToListAsync();

            // Dictionary to track variations that depend on this one
            Dictionary<Guid, string> dependentVariations = new Dictionary<Guid, string>();
            
            foreach (var deliverable in originatingDeliverables)
            {
                await FindDependentVariationsAsync(deliverable, variationGuid, dependentVariations);
            }
            
            if (dependentVariations.Any())
            {
                // Build a clean, user-friendly error message with just the dependent variation numbers/names
                var variationsList = string.Join(", ", dependentVariations.Select(v => $"Variation {v.Value}"));
                
                throw new InvalidOperationException(
                    $"Cannot reject this variation because its deliverables are used by other variations: {variationsList}");
            }
        }
        
        // Helper method to find variations that depend on a specific deliverable
        private async Task FindDependentVariationsAsync(DELIVERABLE deliverable, Guid variationGuid, Dictionary<Guid, string> dependentVariations)
        {
            // Find deliverables in other variations that depend on this one
            var dependentDeliverables = await _context.DELIVERABLEs
                .Where(d => d.GUID_ORIGINAL_DELIVERABLE == deliverable.GUID && 
                          d.GUID_VARIATION != variationGuid && 
                          d.DELETED == null)
                .ToListAsync();
            
            // Collect information about the dependent variations that are approved
            foreach (var dependent in dependentDeliverables)
            {
                if (dependent.GUID_VARIATION.HasValue && !dependentVariations.ContainsKey(dependent.GUID_VARIATION.Value))
                {
                    // Check if the dependent variation is approved
                    var dependentVariation = await _context.VARIATIONs
                        .Where(v => v.GUID == dependent.GUID_VARIATION)
                        .FirstOrDefaultAsync();
                        
                    // Only include variations that are approved (CLIENT_APPROVED not null)
                    if (dependentVariation != null && dependentVariation.CLIENT_APPROVED != null)
                    {
                        dependentVariations.Add(dependent.GUID_VARIATION.Value, dependentVariation.NAME ?? "Unknown");
                    }
                }
            }
        }
        
        // Helper method to update document numbers when rejecting a variation
        private Task UpdateDocumentNumberForRejectionAsync(DELIVERABLE deliverable, Guid variationGuid)
        {
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
            
            return Task.CompletedTask;
        }
        
        // Helper method to process standard variation deliverable rejection
        private async Task ProcessVariationDeliverableRejectionAsync(DELIVERABLE deliverable, Guid variationGuid)
        {
            // Revert status to unapproved
            deliverable.VARIATION_STATUS = (int)VariationStatus.UnapprovedVariation;

            // Update document numbers for self-referencing deliverables
            await UpdateDocumentNumberForRejectionAsync(deliverable, variationGuid);
            
            // Reset approved variation hours on original deliverable
            if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
            {
                // Find original deliverable to update its approved variation hours
                var originalDeliverable = await _context.DELIVERABLEs
                    .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                    
                if (originalDeliverable != null)
                {
                    await ResetApprovedVariationHoursAsync(deliverable, originalDeliverable, variationGuid);
                }
            }
        }
        
        // Helper method to reset approved variation hours during rejection
        private Task ResetApprovedVariationHoursAsync(DELIVERABLE deliverable, DELIVERABLE originalDeliverable, Guid variationGuid)
        {
            // Subtract the hours that were previously added
            decimal currentApprovedHours = originalDeliverable.APPROVED_VARIATION_HOURS;
            decimal variationHours = deliverable.VARIATION_HOURS;
            
            // Check if this is a self-referencing deliverable (added directly through variation)
            bool isSelfReferencing = deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue && 
                                    deliverable.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE.Value && 
                                    deliverable.GUID_VARIATION == variationGuid;
            
            // Prevent negative approved variation hours
            decimal updatedHours = Math.Max(0, currentApprovedHours - variationHours);
            originalDeliverable.APPROVED_VARIATION_HOURS = updatedHours;
            
            // Calculate rate and cost based on deliverable type
            if (isSelfReferencing)
            {
                // For deliverables added directly through variation
                decimal rate = CalculateRateForSelfReferencingDeliverable(deliverable);
                
                // For self-referencing, use the variation hours directly
                originalDeliverable.TOTAL_COST = deliverable.VARIATION_HOURS * rate;
            }
            else
            {
                // For normal variation copies, calculate rate from original deliverable
                decimal existingTotalHours = originalDeliverable.BUDGET_HOURS + currentApprovedHours;
                decimal rate = existingTotalHours > 0 ? originalDeliverable.TOTAL_COST / existingTotalHours : 0;
                
                // Update total cost based on budget + approved variation hours
                decimal totalHours = originalDeliverable.BUDGET_HOURS + updatedHours;
                originalDeliverable.TOTAL_COST = totalHours * rate;
            }
            
            originalDeliverable.UPDATED = DateTime.Now;
            originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            return Task.CompletedTask;
        }
        
        // Helper method to process cancellation deliverable rejection
        private async Task ProcessCancellationDeliverableRejectionAsync(DELIVERABLE deliverable)
        {
            // Revert status to unapproved
            deliverable.VARIATION_STATUS = (int)VariationStatus.UnapprovedCancellation;
            
            // Reset approved variation hours for cancellations
            if (deliverable.GUID_ORIGINAL_DELIVERABLE.HasValue)
            {
                // Find original deliverable to reset its approved variation hours
                var originalDeliverable = await _context.DELIVERABLEs
                    .FirstOrDefaultAsync(d => d.GUID == deliverable.GUID_ORIGINAL_DELIVERABLE && d.DELETED == null);
                    
                if (originalDeliverable != null)
                {
                    await RestoreCancellationHoursAsync(deliverable, originalDeliverable);
                }
            }
        }
        
        // Helper method to restore hours during cancellation rejection
        private Task RestoreCancellationHoursAsync(DELIVERABLE deliverable, DELIVERABLE originalDeliverable)
        {
            // Calculate the current rate (cost per hour) before making any changes
            decimal totalHours = originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS;
            decimal rate = totalHours > 0 ? originalDeliverable.TOTAL_COST / totalHours : 0;
            
            // Restore the tracked delta from the variation to the original deliverable
            // This undoes the effect that the approved cancellation had on the original
            originalDeliverable.APPROVED_VARIATION_HOURS += deliverable.APPROVED_VARIATION_HOURS;
            
            // Update total cost based on the rate and new total hours
            originalDeliverable.TOTAL_COST = (originalDeliverable.BUDGET_HOURS + originalDeliverable.APPROVED_VARIATION_HOURS) * rate;
            
            // Clear the delta from the variation to prevent potential reuse
            deliverable.APPROVED_VARIATION_HOURS = 0;
                
            originalDeliverable.UPDATED = DateTime.Now;
            originalDeliverable.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            return Task.CompletedTask;
        }
        
        // Main method to reject a variation and its deliverables
        public async Task<VARIATION> RejectVariationAsync(Guid variationGuid)
        {
            // Validate and get the variation to reject
            var variation = await GetVariationForRejectionAsync(variationGuid);
            
            // Check if any deliverables that originated in this variation are referenced by other variations
            await ValidateVariationDependentsAsync(variationGuid);
            
            // Start a transaction to ensure all updates are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            // Store the original approved values in case we need to restore them
            DateTime? originalApprovedDate = variation.CLIENT_APPROVED;
            Guid? originalApprovedBy = variation.CLIENT_APPROVEDBY;
            
            try
            {
                // Mark the variation as rejected (by clearing approval)
                variation.CLIENT_APPROVED = null;
                variation.CLIENT_APPROVEDBY = null;
                variation.UPDATED = DateTime.Now;
                variation.UPDATEDBY = _user.UserId ?? Guid.Empty;

                // Get all deliverables related to this variation
                var deliverables = await _context.DELIVERABLEs
                    .Where(d => d.GUID_VARIATION == variationGuid && d.DELETED == null)
                    .ToListAsync();

                // Process each deliverable based on its type
                foreach (var deliverable in deliverables)
                {
                    if (deliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedVariation)
                    {
                        await ProcessVariationDeliverableRejectionAsync(deliverable, variationGuid);
                    }
                    else if (deliverable.VARIATION_STATUS == (int)VariationStatus.ApprovedCancellation)
                    {
                        await ProcessCancellationDeliverableRejectionAsync(deliverable);
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
