using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IVariationRepository
    {
        Task<IEnumerable<VARIATION>> GetAllAsync();
        Task<VARIATION?> GetByIdAsync(Guid id);
        Task<IEnumerable<VARIATION>> GetByProjectIdAsync(Guid projectId);
        Task<VARIATION> CreateAsync(VARIATION variation);
        Task<VARIATION> UpdateAsync(VARIATION variation);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
