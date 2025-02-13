using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<UserEntity> Query();
        Task<OperationResult<UserEntity?>> CreateUser(UserEntity? entity);
        Task<OperationResult<UserEntity?>> UpdateUser(Guid key, Action<UserEntity> update);
        Task<OperationResult<UserEntity?>> UpdateUser(UserEntity? entity);
        Task<OperationResult> DeleteUser(Guid key);
    }
}
