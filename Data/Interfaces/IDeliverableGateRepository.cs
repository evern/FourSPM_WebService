using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IDeliverableGateRepository
    {
        Task<IEnumerable<DELIVERABLE_GATE>> GetAllAsync();
        Task<DELIVERABLE_GATE?> GetByIdAsync(Guid id);
        Task<DELIVERABLE_GATE> CreateAsync(DELIVERABLE_GATE deliverableGate, Guid? createdBy);
        Task<DELIVERABLE_GATE> UpdateAsync(DELIVERABLE_GATE deliverableGate, Guid? updatedBy);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
