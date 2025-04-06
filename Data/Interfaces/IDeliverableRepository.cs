using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IDeliverableRepository
    {
        // Existing methods
        Task<IEnumerable<DELIVERABLE>> GetAllAsync();
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
        /// Retrieves all deliverables for a specific variation, prioritizing variation versions over standard versions
        /// </summary>
        /// <param name="variationId">The GUID of the variation</param>
        /// <returns>
        /// Collection of deliverables that includes:
        /// 1. All deliverables directly associated with the variation
        /// 2. Standard deliverables (with no variation) that don't have a variation version
        /// This ensures that if a standard deliverable has a variation version, only the variation version is returned
        /// </returns>
        Task<IEnumerable<DELIVERABLE>> GetByVariationIdAsync(Guid variationId);
        
        /// <summary>
        /// Gets the variation copy of a deliverable if it exists
        /// </summary>
        /// <param name="originalDeliverableId">The GUID of the original deliverable</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <returns>The variation copy of the deliverable, or null if none exists</returns>
        Task<DELIVERABLE?> GetVariationCopyAsync(Guid originalDeliverableId, Guid variationId);
        
        /// <summary>
        /// Creates a variation copy of an existing deliverable
        /// </summary>
        /// <param name="original">The original deliverable to create a copy from</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <param name="variationStatus">The variation status (UnapprovedVariation or UnapprovedCancellation)</param>
        /// <returns>The newly created variation copy</returns>
        Task<DELIVERABLE> CreateVariationCopyAsync(DELIVERABLE original, Guid variationId, int variationStatus);
        
        /// <summary>
        /// Creates a new deliverable for a variation (not a copy of an existing one)
        /// </summary>
        /// <param name="deliverable">The new deliverable to create</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <returns>The newly created deliverable</returns>
        Task<DELIVERABLE> CreateNewVariationDeliverableAsync(DELIVERABLE deliverable, Guid variationId);
        
        /// <summary>
        /// Creates a variation deliverable that cancels an existing deliverable
        /// </summary>
        /// <param name="originalDeliverableId">The GUID of the deliverable to cancel</param>
        /// <param name="variationId">The GUID of the variation</param>
        /// <returns>The newly created cancellation deliverable</returns>
        Task<DELIVERABLE> CreateVariationCancellationAsync(Guid originalDeliverableId, Guid variationId);
    }
}
