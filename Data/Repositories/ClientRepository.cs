using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Models.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Data.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly FourSPMContext _context;

        public ClientRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CLIENT>> GetAllAsync()
        {
            return await _context.CLIENTs
                .Where(c => c.DELETED == null)
                .OrderByDescending(c => c.CREATED)
                .ToListAsync();
        }
        
        public async Task<CLIENT?> GetByIdAsync(Guid id)
        {
            return await _context.CLIENTs
                .FirstOrDefaultAsync(c => c.GUID == id && c.DELETED == null);
        }

        public async Task<CLIENT> CreateAsync(CLIENT client, Guid? createdBy)
        {
            client.CREATED = DateTime.Now;
            client.CREATEDBY = createdBy ?? Guid.Empty;

            _context.CLIENTs.Add(client);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(client.GUID) ?? client;
        }
        
        public async Task<CLIENT> UpdateAsync(CLIENT client, Guid? updatedBy)
        {
            // Update audit fields directly on the passed client object
            client.UPDATED = DateTime.Now;
            client.UPDATEDBY = updatedBy ?? Guid.Empty;

            try
            {
                await _context.SaveChangesAsync();
                return client;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.CLIENTs.AnyAsync(c => c.GUID == client.GUID && c.DELETED == null))
                {
                    throw new KeyNotFoundException($"Client with ID {client.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }
        
        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var client = await _context.CLIENTs
                .FirstOrDefaultAsync(c => c.GUID == id && c.DELETED == null);
            
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

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.CLIENTs
                .AnyAsync(c => c.GUID == id && c.DELETED == null);
        }
    }
}
