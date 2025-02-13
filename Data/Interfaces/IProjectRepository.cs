using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IProjectRepository
    {
        IQueryable<ProjectEntity> ProjectQuery();
        Task<OperationResult<ProjectEntity?>> CreateProject(ProjectEntity? entity);
        Task<OperationResult<ProjectEntity?>> UpdateProject(Guid key, Action<ProjectEntity> update);
        Task<OperationResult<ProjectEntity?>> UpdateProject(ProjectEntity? entity);
        Task<OperationResult> DeleteProject(Guid key);
    }
}
