using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class VariationRepository : IVariationRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public VariationRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<VARIATION>> GetAllAsync()
        {
            return await _context.VARIATIONs
                .Where(v => v.DELETED == null)
                .Include(v => v.Project)
                .OrderByDescending(v => v.CREATED)
                .ToListAsync();
        }

        public async Task<VARIATION?> GetByIdAsync(Guid id)
        {
            return await _context.VARIATIONs
                .Where(v => v.GUID == id && v.DELETED == null)
                .Include(v => v.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<VARIATION>> GetByProjectIdAsync(Guid projectId)
        {
            return await _context.VARIATIONs
                .Where(v => v.GUID_PROJECT == projectId && v.DELETED == null)
                .Include(v => v.Project)
                .OrderByDescending(v => v.CREATED)
                .ToListAsync();
        }

        public async Task<VARIATION> CreateAsync(VARIATION variation)
        {
            variation.GUID = Guid.NewGuid();
            variation.CREATED = DateTime.Now;
            variation.CREATEDBY = _user.UserId ?? Guid.Empty;

            // Update date/user fields
            SyncDateAndUserFields(variation);

            _context.VARIATIONs.Add(variation);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(variation.GUID) ?? variation;
        }

        public async Task<VARIATION> UpdateAsync(VARIATION variation)
        {
            // Update audit fields directly on the passed object
            variation.UPDATED = DateTime.Now;
            variation.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return variation;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.VARIATIONs.AnyAsync(v => v.GUID == variation.GUID && v.DELETED == null))
                {
                    throw new KeyNotFoundException($"Variation with ID {variation.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            // Check if the variation exists and isn't already deleted
            if (!await _context.VARIATIONs.AnyAsync(v => v.GUID == id && v.DELETED == null))
                return false;

            // Use ExecuteUpdateAsync instead of tracked entities to avoid OUTPUT clause issues with triggers
            // This directly generates a SQL UPDATE command without the OUTPUT clause
            var affectedRows = await _context.VARIATIONs
                .Where(v => v.GUID == id && v.DELETED == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.DELETED, DateTime.Now)
                    .SetProperty(v => v.DELETEDBY, deletedBy)
                );

            return affectedRows > 0;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.VARIATIONs
                .AnyAsync(v => v.GUID == id && v.DELETED == null);
        }

        /// <summary>
        /// Synchronizes date fields with their corresponding user fields
        /// </summary>
        /// <param name="variation">The variation to update</param>
        private void SyncDateAndUserFields(VARIATION variation)
        {
            // Handle SUBMITTED date and user association
            if (variation.SUBMITTED.HasValue)
            {
                // If SUBMITTED date exists but SUBMITTEDBY is not set or empty, set it to current user
                if (!variation.SUBMITTEDBY.HasValue || variation.SUBMITTEDBY == Guid.Empty)
                {
                    variation.SUBMITTEDBY = _user.UserId ?? Guid.Empty;
                }
            }
            else
            {
                // If SUBMITTED date is null, also clear the SUBMITTEDBY field
                variation.SUBMITTEDBY = null;
            }

            // Handle CLIENT_APPROVED date and user association
            if (variation.CLIENT_APPROVED.HasValue)
            {
                // If CLIENT_APPROVED date exists but CLIENT_APPROVEDBY is not set or empty, set it to current user
                if (!variation.CLIENT_APPROVEDBY.HasValue || variation.CLIENT_APPROVEDBY == Guid.Empty)
                {
                    variation.CLIENT_APPROVEDBY = _user.UserId ?? Guid.Empty;
                }
            }
            else
            {
                // If CLIENT_APPROVED date is null, also clear the CLIENT_APPROVEDBY field
                variation.CLIENT_APPROVEDBY = null;
            }
        }
    }
}
