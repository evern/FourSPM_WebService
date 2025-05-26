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

        public ProjectRepository(FourSPMContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PROJECT>> GetAllAsync()
        {
            return await _context.PROJECTs
                .Where(p => p.DELETED == null)
                .Include(p => p.Client)
                .OrderByDescending(p => p.CREATED)
                .ToListAsync();
        }

        public async Task<IEnumerable<PROJECT>> GetAllWithClientsAsync()
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

        public async Task<PROJECT?> GetProjectWithClientAsync(Guid id)
        {
            return await _context.PROJECTs
                .Where(p => p.GUID == id && p.DELETED == null)
                .Include(p => p.Client)
                .FirstOrDefaultAsync();
        }

        public async Task<PROJECT> CreateAsync(PROJECT project, Guid? createdBy)
        {
            project.CREATED = DateTime.Now;
            project.CREATEDBY = createdBy ?? Guid.Empty;

            _context.PROJECTs.Add(project);
            await _context.SaveChangesAsync();

            return project;
        }

        public async Task<PROJECT> UpdateAsync(PROJECT project, Guid? updatedBy)
        {
            // Update audit fields directly on the passed object
            project.UPDATED = DateTime.Now;
            project.UPDATEDBY = updatedBy ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return project;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.PROJECTs.AnyAsync(p => p.GUID == project.GUID && p.DELETED == null))
                {
                    throw new KeyNotFoundException($"Project with ID {project.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
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
