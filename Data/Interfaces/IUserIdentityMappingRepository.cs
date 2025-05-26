using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IUserIdentityMappingRepository
    {
        Task<IEnumerable<USER_IDENTITY_MAPPING>> GetAllAsync();
        Task<USER_IDENTITY_MAPPING?> GetByIdAsync(Guid id);
        Task<USER_IDENTITY_MAPPING?> GetByEmailAsync(string email);
        Task<USER_IDENTITY_MAPPING?> GetByUsernameAsync(string username);
        Task<USER_IDENTITY_MAPPING> CreateAsync(USER_IDENTITY_MAPPING userIdentityMapping);
        Task<USER_IDENTITY_MAPPING> UpdateAsync(USER_IDENTITY_MAPPING userIdentityMapping);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
        Task UpdateLastLoginAsync(Guid id);
    }
}
