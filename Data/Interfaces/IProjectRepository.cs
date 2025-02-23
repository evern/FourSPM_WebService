using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Data.Interfaces
{
    public interface IProjectRepository
    {
        IQueryable<ProjectEntity> ProjectQuery();
        Task<OperationResult<ProjectEntity>> CreateProject(ProjectEntity project);
        Task<OperationResult<ProjectEntity>> UpdateProject(ProjectEntity project);
        Task<OperationResult<ProjectEntity>> UpdateProjectByKey(Guid key, Action<ProjectEntity> update);
        Task<OperationResult> DeleteProject(Guid key);
    }
}
