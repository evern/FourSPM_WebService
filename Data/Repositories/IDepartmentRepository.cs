using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<DEPARTMENT>> GetAllAsync();
        Task<DEPARTMENT?> GetByIdAsync(Guid id);
        Task<DEPARTMENT> CreateAsync(DEPARTMENT department);
        Task<DEPARTMENT> UpdateAsync(DEPARTMENT department);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
