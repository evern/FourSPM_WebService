using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IAreaRepository
    {
        Task<IEnumerable<AREA>> GetAllAsync();
        Task<AREA?> GetByIdAsync(Guid id);
        Task<IEnumerable<AREA>> GetByProjectIdAsync(Guid projectId);
        Task<AREA> CreateAsync(AREA area);
        Task<AREA> UpdateAsync(AREA area);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
