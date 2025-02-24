using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public interface IDeliverableRepository
    {
        Task<IEnumerable<DELIVERABLE>> GetAllAsync();
        Task<IEnumerable<DELIVERABLE>> GetByProjectIdAsync(Guid projectId);
        Task<DELIVERABLE?> GetByIdAsync(Guid id);
        Task<DELIVERABLE> CreateAsync(DELIVERABLE deliverable);
        Task<DELIVERABLE> UpdateAsync(DELIVERABLE deliverable);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
