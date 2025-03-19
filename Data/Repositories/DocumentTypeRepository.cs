using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Models.Session;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly FourSPMContext _context;
        private readonly ApplicationUser _user;

        public DocumentTypeRepository(FourSPMContext context, ApplicationUser user)
        {
            _context = context;
            _user = user;
        }

        public async Task<IEnumerable<DOCUMENT_TYPE>> GetAllAsync()
        {
            return await _context.DOCUMENT_TYPEs
                .Where(d => d.DELETED == null)
                .OrderByDescending(d => d.CREATED)
                .ToListAsync();
        }

        public async Task<DOCUMENT_TYPE?> GetByIdAsync(Guid id)
        {
            return await _context.DOCUMENT_TYPEs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
        }

        public async Task<DOCUMENT_TYPE> CreateAsync(DOCUMENT_TYPE documentType)
        {
            documentType.CREATED = DateTime.Now;
            documentType.CREATEDBY = _user.UserId ?? Guid.Empty;
            
            _context.DOCUMENT_TYPEs.Add(documentType);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(documentType.GUID) ?? documentType;
        }

        public async Task<DOCUMENT_TYPE> UpdateAsync(DOCUMENT_TYPE documentType)
        {
            // Update audit fields directly on the passed object
            documentType.UPDATED = DateTime.Now;
            documentType.UPDATEDBY = _user.UserId ?? Guid.Empty;
            
            try
            {
                await _context.SaveChangesAsync();
                return documentType;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle the case where the entity doesn't exist
                if (!await _context.DOCUMENT_TYPEs.AnyAsync(d => d.GUID == documentType.GUID && d.DELETED == null))
                {
                    throw new KeyNotFoundException($"Document type with ID {documentType.GUID} not found");
                }
                throw; // Rethrow if it's a different issue
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
        {
            var documentType = await _context.DOCUMENT_TYPEs
                .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);

            if (documentType == null)
                return false;

            documentType.DELETED = DateTime.Now;
            documentType.DELETEDBY = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.DOCUMENT_TYPEs
                .AnyAsync(d => d.GUID == id && d.DELETED == null);
        }
    }
}
