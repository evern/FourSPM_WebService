using AutoMapper;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FourSPMContext _context;
        private readonly IMapper _mapper;

        public UserRepository(FourSPMContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public IQueryable<UserEntity> Query() => _mapper.ProjectTo<UserEntity>(Query(_context));

        public static IQueryable<USER> Query(FourSPMContext context) => context.USERs
            .Where(u => !u.DELETED.HasValue)
            .AsSplitQuery();

        public async Task<OperationResult<UserEntity?>> CreateUser(UserEntity? entity, Guid? createdBy)
        {
            if (entity == null)
            {
                return new OperationResult<UserEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = "Missing User parameter."
                };
            }

            if (await _context.USERs.AnyAsync(p => p.FullName == entity.FullName))
            {
                return new OperationResult<UserEntity?>
                {
                    Status = OperationStatus.Validation,
                    Message = $"User {entity.FullName} already exists.",
                    Result = entity
                };
            }

            var efUser = new USER()
            {
                GUID = entity.Guid,
                USERNAME = entity.UserName,
                PASSWORD = entity.Password,
                CREATED = DateTime.Now,
                CREATEDBY = createdBy ?? Guid.Empty
            };

            await _context.USERs.AddAsync(efUser);

            await UpdateUser(efUser, entity, createdBy);

            await _context.SaveChangesAsync();

            return new OperationResult<UserEntity?>
            {
                Status = OperationStatus.Created,
                Result = entity
            };
        }

        public async Task<OperationResult<UserEntity?>> UpdateUserByKey(Guid key, Action<UserEntity> update, Guid? updatedBy)
        {
            var original = await Query().FirstOrDefaultAsync(x => x.Guid == key);

            if (original == null)
            {
                return new OperationResult<UserEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No User found with id {key}."
                };
            }
            update(original);

            // Call the other method by the new name
            return await UpdateUser(original, updatedBy);
        }

        public async Task<OperationResult<UserEntity?>> UpdateUser(UserEntity? entity, Guid? updatedBy)
        {
            if (entity == null)
            {
                return new OperationResult<UserEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = "Missing User parameter."
                };
            }

            var efUser = await _context.USERs.FirstOrDefaultAsync(p => p.GUID == entity.Guid);

            if (efUser is null)
            {
                return new OperationResult<UserEntity?>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"User {entity.FullName} not found.",
                };
            }

            await UpdateUser(efUser, entity, updatedBy);
            await _context.SaveChangesAsync();

            return new OperationResult<UserEntity?>
            {
                Status = OperationStatus.Updated,
                Result = entity
            };
        }

        public async Task<OperationResult> DeleteUser(Guid key, Guid? deletedBy)
        {
            var efUser = await _context.USERs.FirstOrDefaultAsync(p => p.GUID == key);

            if (efUser == null)
            {
                return new OperationResult
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No User found with id {key}."
                };
            }

            efUser.DELETED = DateTime.Now;
            efUser.DELETEDBY = deletedBy ?? Guid.Empty;

            await _context.SaveChangesAsync();
            return OperationResult.Success();
        }

        private Task UpdateUser(USER efUser, UserEntity entity, Guid? updatedBy)
        {
            efUser.FIRST_NAME = entity.FirstName;
            efUser.LAST_NAME = entity.LastName;
            efUser.UPDATED = DateTime.Now;
            efUser.UPDATEDBY = updatedBy ?? Guid.Empty;

            return Task.CompletedTask;
        }
    }
}
