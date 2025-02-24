using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public interface IDeliverableTypeRepository
    {
        Task<IEnumerable<DELIVERABLE_TYPE>> GetAllAsync();
        Task<DELIVERABLE_TYPE?> GetByIdAsync(Guid id);
        Task<DELIVERABLE_TYPE> CreateAsync(DELIVERABLE_TYPE deliverableType);
        Task<DELIVERABLE_TYPE> UpdateAsync(DELIVERABLE_TYPE deliverableType);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
