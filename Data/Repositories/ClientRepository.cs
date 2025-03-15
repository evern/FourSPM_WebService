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

        public async Task<IEnumerable<CLIENT>> GetAllAsync()
        {
            return await _context.CLIENTs
                .Where(c => c.DELETED == null)
                .ToListAsync();
        }
        
        public async Task<CLIENT?> GetByIdAsync(Guid id)
        {
            return await _context.CLIENTs
                .Where(c => c.GUID == id && c.DELETED == null)
                .FirstOrDefaultAsync();
        }

        public async Task<CLIENT> CreateAsync(CLIENT client)
        {
            client.CREATED = DateTime.Now;
            client.CREATEDBY = _user.UserId!.Value;
            
            _context.CLIENTs.Add(client);
            await _context.SaveChangesAsync();
            
            return client;
        }
        
        public async Task<CLIENT> UpdateAsync(CLIENT client)
        {
            var existingClient = await _context.CLIENTs.FirstOrDefaultAsync(c => c.GUID == client.GUID);
            
            if (existingClient == null)
            {
                throw new KeyNotFoundException($"Client with ID {client.GUID} not found");
            }
            
            // Preserve creation info
            client.CREATED = existingClient.CREATED;
            client.CREATEDBY = existingClient.CREATEDBY;
            
            // Update modification info
            client.UPDATED = DateTime.Now;
            client.UPDATEDBY = _user.UserId!.Value;
            
            // Keep deletion info if present
            client.DELETED = existingClient.DELETED;
            client.DELETEDBY = existingClient.DELETEDBY;
            
            _context.Entry(existingClient).CurrentValues.SetValues(client);
            await _context.SaveChangesAsync();
            
            return client;
        }
        
        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var client = await _context.CLIENTs.FirstOrDefaultAsync(c => c.GUID == id);
            
            if (client == null)
            {
                return false;
            }
            
            // Check if there are any associated projects
            var hasProjects = await _context.PROJECTs
                .Where(p => p.GUID_CLIENT == id && p.DELETED == null)
                .AnyAsync();
            
            if (hasProjects)
            {
                throw new InvalidOperationException("Cannot delete client with associated projects.");
            }
            
            // Soft delete
            client.DELETED = DateTime.Now;
            client.DELETEDBY = deletedBy;
            
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}
