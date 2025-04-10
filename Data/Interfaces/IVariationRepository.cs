using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IVariationRepository
    {
        Task<IEnumerable<VARIATION>> GetAllAsync();
        Task<VARIATION?> GetByIdAsync(Guid id);
        Task<IEnumerable<VARIATION>> GetByProjectIdAsync(Guid projectId);
        Task<VARIATION> CreateAsync(VARIATION variation);
        Task<VARIATION> UpdateAsync(VARIATION variation);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
        
        /// <summary>
        /// Approves a variation, updating all variation deliverables to approved status
        /// </summary>
        /// <param name="variationGuid">The GUID of the variation to approve</param>
        /// <returns>The updated variation entity with approval information</returns>
        Task<VARIATION> ApproveVariationAsync(Guid variationGuid);
        
        /// <summary>
        /// Rejects a previously approved variation, reverting all variation deliverables to unapproved status
        /// </summary>
        /// <param name="variationGuid">The GUID of the variation to reject</param>
        /// <returns>The updated variation entity with approval information cleared</returns>
        Task<VARIATION> RejectVariationAsync(Guid variationGuid);
    }
}
