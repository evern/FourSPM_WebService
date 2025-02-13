using AutoMapper;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Queries;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{

    public class ProjectRepository : IProjectRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;
        private readonly IMapper _mapper;

        public ProjectRepository(FourSPMContext context, ApplicationUser user, IMapper mapper)
        {
            _context = context;
            _user = user;
            _mapper = mapper;
        }

        public IQueryable<ProjectEntity> ProjectQuery()
        {
            var query = ProjectQueries.UserProjectQuery(_context, _user);

            return _mapper.ProjectTo<ProjectEntity>(query);
        }

        private IQueryable<PROJECT> ProjectFull =>
            _context.PROJECTs
                .AsSplitQuery();

        public async Task<OperationResult<ProjectEntity?>> CreateProject(ProjectEntity? entity)
        {
            if (entity == null)
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = "Missing project parameter."
                };
            }

            if (await _context.PROJECTs.AnyAsync(p => p.GUID == entity.Guid))
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.Validation,
                    Message = $"Project {entity.Number} already exists.",
                    Result = entity
                };
            }

            var efProject = new PROJECT
            {
                GUID = entity.Guid,
                CREATED = DateTime.Now,
                CREATEDBY = _user.UserId!.Value
            };


            await _context.PROJECTs.AddAsync(efProject);

            await UpdateProject(efProject, entity);

            await _context.SaveChangesAsync();

            return new OperationResult<ProjectEntity?>
            {
                Status = OperationStatus.Created,
                Result = entity
            };
        }

        public async Task<OperationResult<ProjectEntity?>> UpdateProject(Guid key, Action<ProjectEntity> update)
        {
            var original = await ProjectQuery().FirstOrDefaultAsync(x => x.Id == key);

            if (original == null)
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No project found with id {key}."
                };
            }

            update(original);

            return await UpdateProject(original);
        }

        public async Task<OperationResult<ProjectEntity?>> UpdateProject(ProjectEntity? entity)
        {
            if (entity == null)
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = "Missing project parameter."
                };
            }

            var efProject = await ProjectFull.FirstOrDefaultAsync(p => p.GUID == entity.Guid);

            if (efProject is null)
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"Project {entity.Number} not found.",
                };
            }

            if (!ProjectQueries.UserProjectQuery(_context, _user).Contains(efProject))
            {
                return new OperationResult<ProjectEntity?>
                {
                    Status = OperationStatus.NoAccess,
                    Message = $"{_user.Upn} does not have access to project '{efProject.NUMBER}'."
                };
            }

            await UpdateProject(efProject, entity);

            return new OperationResult<ProjectEntity?>
            {
                Status = OperationStatus.Updated,
                Result = entity
            };
        }

        public async Task<OperationResult> DeleteProject(Guid key)
        {
            var efProject = await _context.PROJECTs.FirstOrDefaultAsync(p => p.GUID == key);
            var securityQuery = ProjectQueries.UserProjectQuery(_context, _user);

            if (efProject == null)
            {
                return new OperationResult
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No project found with id {key}."
                };
            }

            if (!securityQuery.Contains(efProject))
            {
                return new OperationResult
                {
                    Status = OperationStatus.NoAccess,
                    Message = $"{_user.Upn} does not have access to project '{efProject.NUMBER}'."
                };
            }

            efProject.DELETED = DateTime.Now;
            efProject.DELETEDBY = _user.UserId!.Value;

            await _context.SaveChangesAsync();

            return OperationResult.Success();
        }

        private Task UpdateProject(PROJECT efProject, ProjectEntity entity)
        {
            efProject.NUMBER = entity.Number;
            efProject.NAME = entity.Name;
            efProject.CLIENT = entity.Client;

            return Task.CompletedTask;
        }
    }
}
