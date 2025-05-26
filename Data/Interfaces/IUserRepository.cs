using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<UserEntity> Query();
        Task<OperationResult<UserEntity?>> CreateUser(UserEntity? entity, Guid? createdBy);
        Task<OperationResult<UserEntity?>> UpdateUserByKey(Guid key, Action<UserEntity> update, Guid? updatedBy);
        Task<OperationResult<UserEntity?>> UpdateUser(UserEntity? entity, Guid? updatedBy);
        Task<OperationResult> DeleteUser(Guid key, Guid? deletedBy);
    }
}
