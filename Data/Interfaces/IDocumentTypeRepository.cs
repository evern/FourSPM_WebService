using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IDocumentTypeRepository
    {
        Task<DOCUMENT_TYPE?> GetByIdAsync(Guid id);
        Task<IEnumerable<DOCUMENT_TYPE>> GetAllAsync();
        Task<DOCUMENT_TYPE> CreateAsync(DOCUMENT_TYPE documentType);
        Task<DOCUMENT_TYPE> UpdateAsync(DOCUMENT_TYPE documentType);
        Task<bool> DeleteAsync(Guid id, Guid deletedBy);
        Task<bool> ExistsAsync(Guid id);
    }
}
