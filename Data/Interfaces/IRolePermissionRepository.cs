using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IRolePermissionRepository
    {
        Task<ROLE_PERMISSION?> GetByIdAsync(int id);
        Task<IEnumerable<ROLE_PERMISSION>> GetAllAsync();
        Task<IEnumerable<ROLE_PERMISSION>> GetByRoleIdAsync(int roleId);
        Task<ROLE_PERMISSION> CreateAsync(ROLE_PERMISSION rolePermission);
        Task<ROLE_PERMISSION> UpdateAsync(ROLE_PERMISSION rolePermission);
        Task<bool> DeleteAsync(int id, string deletedBy);
        Task<bool> ExistsAsync(int id);
    }
}
