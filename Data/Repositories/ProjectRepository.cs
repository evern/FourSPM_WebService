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
