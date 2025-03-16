using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public AreaRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<AREA>> GetAllAsync()
        {
            return await _context.AREAs
                .Where(a => a.DELETED == null)
                .Include(a => a.Project)
                .OrderByDescending(a => a.CREATED)
                .ToListAsync();
        }

        public async Task<AREA?> GetByIdAsync(Guid id)
        {
            return await _context.AREAs
                .Where(a => a.GUID == id && a.DELETED == null)
                .Include(a => a.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AREA>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.AREAs
                .Where(a => a.GUID_PROJECT == projectId && a.DELETED == null)
                .Include(a => a.Project)
                .OrderByDescending(a => a.CREATED)
                .ToListAsync();
        }

        public async Task<AREA> CreateAsync(AREA area)
        {
            area.GUID = Guid.NewGuid();
            area.CREATED = DateTime.Now;
            area.CREATEDBY = _user.UserId ?? Guid.Empty;

            _context.AREAs.Add(area);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(area.GUID) ?? area;
        }

        public async Task<AREA> UpdateAsync(AREA area)
        {
            var existingArea = await _context.AREAs
                .FirstOrDefaultAsync(a => a.GUID == area.GUID && a.DELETED == null);

            if (existingArea == null)
                throw new KeyNotFoundException($"Area with ID {area.GUID} not found");

            // Update individual properties
            existingArea.NUMBER = area.NUMBER;
            existingArea.DESCRIPTION = area.DESCRIPTION;
            existingArea.UPDATED = DateTime.Now;
            existingArea.UPDATEDBY = _user.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(existingArea.GUID) ?? existingArea;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var area = await _context.AREAs.FindAsync(id);
            if (area == null || area.DELETED != null)
                return false;

            area.DELETED = DateTime.Now;
            area.DELETEDBY = deletedBy;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.AREAs
                .AnyAsync(a => a.GUID == id && a.DELETED == null);
        }
    }
}
