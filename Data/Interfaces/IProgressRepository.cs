using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IProgressRepository
    {
        Task<IEnumerable<PROGRESS>> GetAllAsync();
        Task<IEnumerable<PROGRESS>> GetByDeliverableIdAsync(Guid deliverableId);
        Task<PROGRESS?> GetByIdAsync(Guid id);
        Task<PROGRESS> CreateAsync(PROGRESS progress, Guid? createdBy);
        Task<PROGRESS> UpdateAsync(PROGRESS progress, Guid? updatedBy);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
        
        
        /// <summary>
        /// Checks if a deliverable has been progressed (sum of units > 0)
        /// </summary>
        /// <param name="deliverableGuid">The GUID of the deliverable to check</param>
        /// <returns>True if the deliverable has been progressed (sum of units > 0), false otherwise</returns>
        Task<bool> HasProgressUnitsAsync(Guid deliverableGuid);
    }
}
