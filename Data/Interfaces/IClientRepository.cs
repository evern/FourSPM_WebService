using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IClientRepository
    {
        Task<IEnumerable<CLIENT>> GetAllAsync();
        Task<CLIENT?> GetByIdAsync(Guid id);
        Task<CLIENT> CreateAsync(CLIENT client);
        Task<CLIENT> UpdateAsync(CLIENT client);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
