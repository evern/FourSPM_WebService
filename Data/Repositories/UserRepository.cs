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
        private readonly ApplicationUser _user;
        private readonly IMapper _mapper;

        public UserRepository(ApplicationUser user, FourSPMContext context, IMapper mapper)
        {
            _user = user;
            _context = context;
            _mapper = mapper;
        }

        public IQueryable<UserEntity> Query() => _mapper.ProjectTo<UserEntity>(Query(_context));

        public static IQueryable<USER> Query(FourSPMContext context) => context.USERs
            .Where(u => !u.DELETED.HasValue)
            .AsSplitQuery();

        public async Task<OperationResult<UserEntity?>> CreateUser(UserEntity? entity)
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

            var efUser = new USER
            {
                GUID = entity.Guid,
                CREATED = DateTime.Now,
                CREATEDBY = _user.UserId!.Value
            };

            await _context.USERs.AddAsync(efUser);

            await UpdateUser(efUser, entity);

            await _context.SaveChangesAsync();

            return new OperationResult<UserEntity?>
            {
                Status = OperationStatus.Created,
                Result = entity
            };
        }

        public async Task<OperationResult<UserEntity?>> UpdateUserByKey(Guid key, Action<UserEntity> update)
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
            return await UpdateUser(original);
        }

        public async Task<OperationResult<UserEntity?>> UpdateUser(UserEntity? entity)
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

            await UpdateUser(efUser, entity);
            await _context.SaveChangesAsync();

            return new OperationResult<UserEntity?>
            {
                Status = OperationStatus.Updated,
                Result = entity
            };
        }

        public async Task<OperationResult> DeleteUser(Guid key)
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
            efUser.DELETEDBY = _user.UserId!.Value;

            await _context.SaveChangesAsync();
            return OperationResult.Success();
        }

        private Task UpdateUser(USER efUser, UserEntity entity)
        {
            efUser.FIRST_NAME = entity.FirstName;
            efUser.LAST_NAME = entity.LastName;
            efUser.UPDATED = DateTime.Now;
            efUser.UPDATEDBY = _user.UserId!.Value;

            return Task.CompletedTask;
        }
    }
}
