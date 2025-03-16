using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<PROJECT>> GetAllAsync();
        Task<PROJECT?> GetByIdAsync(Guid id);
        Task<PROJECT?> GetProjectWithClientAsync(Guid id);
        Task<PROJECT> CreateAsync(PROJECT project);
        Task<PROJECT> UpdateAsync(PROJECT project);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    }
}
