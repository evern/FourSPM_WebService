using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public interface IProgressRepository
    {
        Task<IEnumerable<PROGRESS>> GetAllAsync();
        Task<IEnumerable<PROGRESS>> GetByDeliverableIdAsync(Guid deliverableId);
        Task<PROGRESS?> GetByIdAsync(Guid id);
        Task<PROGRESS> CreateAsync(PROGRESS progress);
        Task<PROGRESS> UpdateAsync(PROGRESS progress);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
