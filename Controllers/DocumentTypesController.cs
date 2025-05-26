using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FourSPM_WebService.Attributes;
using FourSPM_WebService.Data.Constants;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DocumentTypesController : FourSPMODataController
    {
        private readonly IDocumentTypeRepository _repository;
        private readonly ILogger<DocumentTypesController> _logger;

        public DocumentTypesController(
            IDocumentTypeRepository repository,
            ILogger<DocumentTypesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.DocumentTypesView)]
        public async Task<IActionResult> Get()
        {
            var documentTypes = await _repository.GetAllAsync();
            var entities = documentTypes.Select(d => MapToEntity(d));
            return Ok(entities);
        }

        [EnableQuery]
        [RequirePermission(PermissionConstants.DocumentTypesView)]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var documentType = await _repository.GetByIdAsync(key);
            if (documentType == null)
                return NotFound();

            return Ok(MapToEntity(documentType));
        }

        [RequirePermission(PermissionConstants.DocumentTypesEdit)]
        public async Task<IActionResult> Post([FromBody] DocumentTypeEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var documentType = new DOCUMENT_TYPE
            {
                GUID = entity.Guid,
                CODE = entity.Code,
                NAME = entity.Name ?? string.Empty
            };

            var result = await _repository.CreateAsync(documentType, CurrentUser.UserId);
            return Created(MapToEntity(result));
        }

        [RequirePermission(PermissionConstants.DocumentTypesEdit)]
        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DocumentTypeEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var documentType = new DOCUMENT_TYPE
                {
                    GUID = entity.Guid,
                    CODE = entity.Code,
                    NAME = entity.Name ?? string.Empty
                };

                var result = await _repository.UpdateAsync(documentType, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [RequirePermission(PermissionConstants.DocumentTypesEdit)]
        public async Task<IActionResult> Delete([FromRoute] Guid key)
        {
            var result = await _repository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Partially updates a document type
        /// </summary>
        /// <param name="key">The GUID of the document type to update</param>
        /// <param name="delta">The document type properties to update</param>
        /// <returns>The updated document type</returns>
        [RequirePermission(PermissionConstants.DocumentTypesEdit)]
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DocumentTypeEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for document type {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest("Invalid GUID - The document type ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for document type {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing document type
                var existingDocumentType = await _repository.GetByIdAsync(key);
                if (existingDocumentType == null)
                {
                    return NotFound("Document type with ID " + key + " was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingDocumentType);
                delta.CopyChangedValues(updatedEntity);

                // Map back to DOCUMENT_TYPE entity
                existingDocumentType.CODE = updatedEntity.Code;
                existingDocumentType.NAME = updatedEntity.Name ?? string.Empty;

                var result = await _repository.UpdateAsync(existingDocumentType, CurrentUser.UserId);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating document type");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        private static DocumentTypeEntity MapToEntity(DOCUMENT_TYPE documentType)
        {
            return new DocumentTypeEntity
            {
                Guid = documentType.GUID,
                Code = documentType.CODE,
                Name = documentType.NAME,
                Created = documentType.CREATED,
                CreatedBy = documentType.CREATEDBY,
                Updated = documentType.UPDATED,
                UpdatedBy = documentType.UPDATEDBY,
                Deleted = documentType.DELETED,
                DeletedBy = documentType.DELETEDBY
            };
        }
    }
}
