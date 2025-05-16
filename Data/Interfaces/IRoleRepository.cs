using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IRoleRepository
    {
        Task<ROLE?> GetByIdAsync(Guid id);
        Task<IEnumerable<ROLE>> GetAllAsync();
        Task<ROLE> CreateAsync(ROLE role);
        Task<ROLE> UpdateAsync(ROLE role);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
