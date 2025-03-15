using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public ClientRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public IQueryable<ClientEntity> ClientQuery()
        {
            return _context.CLIENTs
                .Where(c => c.DELETED == null)
                .Select(c => new ClientEntity
                {
                    Guid = c.GUID,
                    Number = c.NUMBER,
                    Description = c.DESCRIPTION,
                    ClientContact = c.CLIENT_CONTACT,
                    Created = c.CREATED,
                    CreatedBy = c.CREATEDBY,
                    Updated = c.UPDATED,
                    UpdatedBy = c.UPDATEDBY,
                    Deleted = c.DELETED,
                    DeletedBy = c.DELETEDBY
                });
        }

        public async Task<OperationResult<ClientEntity>> CreateClient(ClientEntity client)
        {
            try
            {
                if (await _context.CLIENTs.AnyAsync(c => c.GUID == client.Guid))
                {
                    return new OperationResult<ClientEntity>
                    {
                        Status = OperationStatus.Validation,
                        Message = $"Client {client.Number} already exists.",
                        Result = client
                    };
                }

                var newClient = new CLIENT
                {
                    GUID = client.Guid,
                    NUMBER = client.Number,
                    DESCRIPTION = client.Description,
                    CLIENT_CONTACT = client.ClientContact,
                    CREATED = DateTime.Now,
                    CREATEDBY = _user.UserId!.Value
                };

                _context.CLIENTs.Add(newClient);
                await _context.SaveChangesAsync();

                return new OperationResult<ClientEntity>
                {
                    Status = OperationStatus.Created,
                    Result = client
                };
            }
            catch (Exception ex)
            {
                return new OperationResult<ClientEntity>
                {
                    Status = OperationStatus.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<OperationResult<ClientEntity>> UpdateClientByKey(Guid key, Action<ClientEntity> update)
        {
            var original = await ClientQuery().FirstOrDefaultAsync(x => x.Guid == key);

            if (original == null)
            {
                return new OperationResult<ClientEntity>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No client found with id {key}."
                };
            }

            update(original);
            return await UpdateClient(original);
        }

        public async Task<OperationResult<ClientEntity>> UpdateClient(ClientEntity client)
        {
            var efClient = await _context.CLIENTs.FirstOrDefaultAsync(c => c.GUID == client.Guid);

            if (efClient == null)
            {
                return new OperationResult<ClientEntity>
                {
                    Status = OperationStatus.NotFound,
                    Message = $"Client {client.Number} not found."
                };
            }

            efClient.NUMBER = client.Number;
            efClient.DESCRIPTION = client.Description;
            efClient.CLIENT_CONTACT = client.ClientContact;
            efClient.UPDATED = DateTime.Now;
            efClient.UPDATEDBY = _user.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync();

            return new OperationResult<ClientEntity>
            {
                Status = OperationStatus.Updated,
                Result = client
            };
        }

        public async Task<OperationResult> DeleteClient(Guid key)
        {
            var client = await _context.CLIENTs.FirstOrDefaultAsync(c => c.GUID == key);

            if (client == null)
            {
                return new OperationResult
                {
                    Status = OperationStatus.NotFound,
                    Message = $"No client found with id {key}."
                };
            }

            // Check if there are any associated projects
            var hasProjects = await _context.PROJECTs
                .Where(p => p.GUID_CLIENT == key && p.DELETED == null)
                .AnyAsync();

            if (hasProjects)
            {
                return new OperationResult
                {
                    Status = OperationStatus.Validation,
                    Message = "Cannot delete client with associated projects."
                };
            }

            client.DELETED = DateTime.Now;
            client.DELETEDBY = _user.UserId;

            await _context.SaveChangesAsync();

            return new OperationResult { Status = OperationStatus.Success };
        }
    }
}
