using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IDeliverableRepository
    {
        // Existing methods
        IQueryable<DELIVERABLE> GetAllAsync();
        Task<IEnumerable<DELIVERABLE>> GetByProjectIdAsync(Guid projectId);
        Task<DELIVERABLE?> GetByIdAsync(Guid id);
        Task<DELIVERABLE> CreateAsync(DELIVERABLE deliverable);
        Task<DELIVERABLE> UpdateAsync(DELIVERABLE deliverable);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<DELIVERABLE>> GetDeliverablesByNumberPatternAsync(Guid projectId, string pattern);
        Task<IEnumerable<DELIVERABLE>> GetByProjectIdAndPeriodAsync(Guid projectId, int period);
        
        // Variation-specific methods
        /// <summary>
        /// Gets merged variation deliverables for a given variation and eligible original deliverables that could be added
        /// Uses pure IQueryable approach for optimal OData integration and virtual scrolling support
        /// </summary>
        /// <param name="variationGuid">Optional variation GUID to filter by. If not provided, returns all variation deliverables.</param>
        /// <returns>IQueryable of deliverables including both variation-specific ones and eligible standards</returns>
        IQueryable<DELIVERABLE> GetMergedVariationDeliverables(Guid? variationGuid = null);
        
        /// <summary>
        /// Gets the variation copy of a deliverable if it exists
        /// </summary>
        /// <param name="originalDeliverableId">The GUID of the original deliverable</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <returns>The variation copy of the deliverable, or null if none exists</returns>
        Task<DELIVERABLE?> FindExistingVariationDeliverableAsync(Guid originalDeliverableId, Guid variationId);
        
        /// <summary>
        /// Creates a variation copy of an existing deliverable
        /// </summary>
        /// <param name="original">The original deliverable to create a copy from</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <param name="variationStatus">The variation status (UnapprovedVariation or UnapprovedCancellation)</param>
        /// <returns>The newly created variation copy</returns>
        Task<DELIVERABLE> CreateVariationCopyAsync(DELIVERABLE original, Guid variationId, int variationStatus);
        
        /// <summary>
        /// Cancels a deliverable by either marking it as deleted or creating a cancellation variation
        /// </summary>
        /// <param name="originalDeliverableGuid">The original deliverable GUID to cancel</param>
        /// <param name="variationGuid">The variation GUID this cancellation belongs to</param>
        /// <returns>The updated or newly created cancellation deliverable</returns>
        Task<DELIVERABLE> CancelDeliverableAsync(Guid originalDeliverableGuid, Guid? variationGuid);
    }
}
