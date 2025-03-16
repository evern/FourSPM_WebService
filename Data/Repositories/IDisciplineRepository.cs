using FourSPM_WebService.Data.EF.FourSPM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public interface IDisciplineRepository
    {
        Task<IEnumerable<DISCIPLINE>> GetAllAsync();
        Task<DISCIPLINE?> GetByIdAsync(Guid id);
        Task<DISCIPLINE?> GetByCodeAsync(string code);
        Task<DISCIPLINE> CreateAsync(DISCIPLINE discipline);
        Task<DISCIPLINE> UpdateAsync(DISCIPLINE discipline);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
