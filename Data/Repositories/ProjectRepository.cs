using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Queries;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<PROJECT>> GetAllAsync()
        {
            return await _context.PROJECTs
                .Where(p => p.DELETED == null)
                .Include(p => p.Client)
                .OrderByDescending(p => p.CREATED)
                .ToListAsync();
        }

        public async Task<PROJECT?> GetByIdAsync(Guid id)
        {
            return await _context.PROJECTs
                .Where(p => p.GUID == id && p.DELETED == null)
                .Include(p => p.Client)
                .FirstOrDefaultAsync();
        }

        public IQueryable<ProjectEntity> ProjectQuery()
        {
            // Updated to include CLIENT relationship
            return _context.PROJECTs
                .Where(p => p.DELETED == null)
                .Select(p => new ProjectEntity
                {
                    Guid = p.GUID,
                    ClientGuid = p.GUID_CLIENT,
                    ProjectNumber = p.PROJECT_NUMBER,
                    Name = p.NAME,
                    PurchaseOrderNumber = p.PURCHASE_ORDER_NUMBER,
                    ProjectStatus = p.PROJECT_STATUS,
                    ProgressStart = p.PROGRESS_START,
                    Created = p.CREATED,
                    CreatedBy = p.CREATEDBY,
                    Updated = p.UPDATED,
                    UpdatedBy = p.UPDATEDBY,
                    Deleted = p.DELETED,
                    DeletedBy = p.DELETEDBY,
                    // Include client data if available
                    Client = p.Client != null ? new ClientEntity
                    {
                        Guid = p.Client.GUID,
                        Number = p.Client.NUMBER,
                        Description = p.Client.DESCRIPTION,
                        ClientContactName = p.Client.CLIENT_CONTACT_NAME,
                        ClientContactNumber = p.Client.CLIENT_CONTACT_NUMBER,
                        ClientContactEmail = p.Client.CLIENT_CONTACT_EMAIL
                    } : null
                });
        }

        public async Task<PROJECT> CreateAsync(PROJECT project)
        {
            project.CREATED = DateTime.Now;
            project.CREATEDBY = _user.UserId ?? Guid.Empty;

            _context.PROJECTs.Add(project);
            await _context.SaveChangesAsync();

            return project;
        }

        public async Task<PROJECT> UpdateAsync(PROJECT project)
        {
            var existingProject = await _context.PROJECTs
                .FirstOrDefaultAsync(p => p.GUID == project.GUID && p.DELETED == null);

            if (existingProject == null)
                throw new KeyNotFoundException($"Project with ID {project.GUID} not found");

            // Update individual properties
            existingProject.GUID_CLIENT = project.GUID_CLIENT;
            existingProject.PROJECT_NUMBER = project.PROJECT_NUMBER;
            existingProject.NAME = project.NAME;
            existingProject.PURCHASE_ORDER_NUMBER = project.PURCHASE_ORDER_NUMBER;
            existingProject.PROJECT_STATUS = project.PROJECT_STATUS;
            existingProject.PROGRESS_START = project.PROGRESS_START;
            existingProject.UPDATED = DateTime.Now;
            existingProject.UPDATEDBY = _user.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(existingProject.GUID) ?? existingProject;
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

                // Validate client exists if ClientGuid is provided
                if (project.ClientGuid.HasValue && !await _context.CLIENTs.AnyAsync(c => c.GUID == project.ClientGuid && c.DELETED == null))
                {
                    return new OperationResult<ProjectEntity>
                    {
                        Status = OperationStatus.Validation,
                        Message = $"Specified client with ID {project.ClientGuid} does not exist.",
                        Result = project
                    };
                }

                var newProject = new PROJECT
                {
                    GUID = project.Guid,
                    GUID_CLIENT = project.ClientGuid,
                    PROJECT_NUMBER = project.ProjectNumber,
                    NAME = project.Name,
                    PURCHASE_ORDER_NUMBER = project.PurchaseOrderNumber,
                    PROJECT_STATUS = project.ProjectStatus,
                    PROGRESS_START = project.ProgressStart,
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

            // Validate client exists if ClientGuid is provided
            if (project.ClientGuid.HasValue && !await _context.CLIENTs.AnyAsync(c => c.GUID == project.ClientGuid && c.DELETED == null))
            {
                return new OperationResult<ProjectEntity>
                {
                    Status = OperationStatus.Validation,
                    Message = $"Specified client with ID {project.ClientGuid} does not exist.",
                    Result = project
                };
            }

            efProject.GUID_CLIENT = project.ClientGuid;
            efProject.PROJECT_NUMBER = project.ProjectNumber;
            efProject.NAME = project.Name;
            efProject.PURCHASE_ORDER_NUMBER = project.PurchaseOrderNumber;
            efProject.PROJECT_STATUS = project.ProjectStatus;
            efProject.PROGRESS_START = project.ProgressStart;
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

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var project = await _context.PROJECTs.FindAsync(id);
            if (project == null || project.DELETED != null)
                return false;

            project.DELETED = DateTime.Now;
            project.DELETEDBY = deletedBy;
            await _context.SaveChangesAsync();
            return true;
        }

        private Task<bool> HasAccess(PROJECT project)
        {
            // For now, everyone has access to all projects
            return Task.FromResult(true);
        }
    }
}
