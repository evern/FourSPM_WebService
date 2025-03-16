using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Repositories
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly FourSPMContext _context;

        public DocumentTypeRepository(FourSPMContext context)
        {
            _context = context;
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
            _context.DOCUMENT_TYPEs.Add(documentType);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(documentType.GUID) ?? documentType;
        }

        public async Task<DOCUMENT_TYPE> UpdateAsync(DOCUMENT_TYPE documentType)
        {
            var existingDocumentType = await _context.DOCUMENT_TYPEs
                .FirstOrDefaultAsync(d => d.GUID == documentType.GUID && d.DELETED == null);

            if (existingDocumentType == null)
                throw new KeyNotFoundException($"Document type with ID {documentType.GUID} not found");

            existingDocumentType.CODE = documentType.CODE;
            existingDocumentType.NAME = documentType.NAME;
            existingDocumentType.UPDATED = DateTime.Now;
            existingDocumentType.UPDATEDBY = documentType.UPDATEDBY;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(existingDocumentType.GUID) ?? existingDocumentType;
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
