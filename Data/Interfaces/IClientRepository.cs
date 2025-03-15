using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IClientRepository
    {
        IQueryable<ClientEntity> ClientQuery();
        Task<OperationResult<ClientEntity>> CreateClient(ClientEntity client);
        Task<OperationResult<ClientEntity>> UpdateClient(ClientEntity client);
        Task<OperationResult<ClientEntity>> UpdateClientByKey(Guid key, Action<ClientEntity> update);
        Task<OperationResult> DeleteClient(Guid key);
    }
}
