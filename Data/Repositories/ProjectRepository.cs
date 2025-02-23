using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Queries;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public ProjectRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public IQueryable<ProjectEntity> ProjectQuery()
        {
            return ProjectQueries.UserProjectQuery(_context, _user);
        }

        public async Task<OperationResult<ProjectEntity>> CreateProject(ProjectEntity project)
        {
            try
            {
                if (await _context.PROJECTs.AnyAsync(p => p.GUID == project.Guid))
                {
                    return new OperationResult<ProjectEntity>
                    {
                        Status = OperationStatus.Validation,
                        Message = $"Project {project.ProjectNumber} already exists.",
                        Result = project
                    };
                }

                var newProject = new PROJECT
                {
                    GUID = project.Guid,
                    CLIENT_NUMBER = project.ClientNumber,
                    PROJECT_NUMBER = project.ProjectNumber,
                    NAME = project.Name,
                    CLIENT_CONTACT = project.ClientContact,
                    PURCHASE_ORDER_NUMBER = project.PurchaseOrderNumber,
                    PROJECT_STATUS = project.ProjectStatus,
                    CREATED = DateTime.Now,
                    CREATEDBY = _user.UserId!.Value
                };

                _context.PROJECTs.Add(newProject);
                await _context.SaveChangesAsync();

                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.Created,
                    Result = project
                };
            }
            catch (Exception ex)
            {
                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<OperationResult<ProjectEntity>> UpdateProjectByKey(Guid key, Action<ProjectEntity> update)
        {
            var original = await ProjectQuery().FirstOrDefaultAsync(x => x.Guid == key);

            if (original == null)
            {
                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No project found with id {key}."
                };
            }

            update(original);
            return await UpdateProject(original);
        }

        public async Task<OperationResult<ProjectEntity>> UpdateProject(ProjectEntity project)
        {
            var efProject = await _context.PROJECTs.FirstOrDefaultAsync(p => p.GUID == project.Guid);

            if (efProject == null)
            {
                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"Project {project.ProjectNumber} not found."
                };
            }

            if (!await HasAccess(efProject))
            {
                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.NoAccess,
                    Message = $"{_user.Upn} does not have access to project '{efProject.PROJECT_NUMBER}'."
                };
            }

            efProject.CLIENT_NUMBER = project.ClientNumber;
            efProject.PROJECT_NUMBER = project.ProjectNumber;
            efProject.CLIENT_CONTACT = project.ClientContact;
            efProject.NAME = project.Name;
            efProject.PURCHASE_ORDER_NUMBER = project.PurchaseOrderNumber;
            efProject.PROJECT_STATUS = project.ProjectStatus;
            efProject.UPDATED = DateTime.Now;
            efProject.UPDATEDBY = _user.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync();

            return new OperationResult<ProjectEntity>
            {
                Status = OperationStatus.Updated,
                Result = project
            };
        }

        public async Task<OperationResult> DeleteProject(Guid key)
        {
            var project = await _context.PROJECTs.FirstOrDefaultAsync(p => p.GUID == key);

            if (project == null)
            {
                return new OperationResult
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No project found with id {key}."
                };
            }

            if (!await HasAccess(project))
            {
                return new OperationResult
                {
                    Status = OperationStatus.NoAccess,
                    Message = $"{_user.Upn} does not have access to project '{project.PROJECT_NUMBER}'."
                };
            }

            project.DELETED = DateTime.Now;
            project.DELETEDBY = _user.UserId;

            await _context.SaveChangesAsync();

            return new OperationResult { Status = OperationStatus.Success };
        }

        private async Task<bool> HasAccess(PROJECT project)
        {
            // For now, everyone has access to all projects
            return true;
        }
    }
}
