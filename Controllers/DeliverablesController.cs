using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Deltas;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Formatter;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DeliverablesController : FourSPMODataController
    {
        private readonly IDeliverableRepository _repository;
        private readonly ILogger<DeliverablesController> _logger;

        public DeliverablesController(IDeliverableRepository repository, ILogger<DeliverablesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public class ODataResponse<T>
        {
            public required IEnumerable<T> Value { get; set; }
            [JsonProperty("@odata.count")]
            public required int Count { get; set; }

            public ODataResponse()
            {
                Value = Enumerable.Empty<T>();
                Count = 0;
            }
        }

        [EnableQuery]
        public async Task<IActionResult> Get()
        {
            var deliverables = await _repository.GetAllAsync();
            var entities = deliverables.Select(d => MapToEntity(d));
            return Ok(entities);
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] Guid key)
        {
            var deliverable = await _repository.GetByIdAsync(key);
            if (deliverable == null)
                return NotFound();

            return Ok(MapToEntity(deliverable));
        }

        [EnableQuery]
        [HttpGet("odata/v1/GetByProject({projectId})")]
        public async Task<IActionResult> GetByProject([FromRoute] Guid projectId)
        {
            var deliverables = await _repository.GetByProjectIdAsync(projectId);
            var entities = deliverables.Select(d => MapToEntity(d)).ToList();
            
            var response = new ODataResponse<DeliverableEntity>
            {
                Value = entities,
                Count = entities.Count
            };

            return Ok(response);
        }

        public async Task<IActionResult> Post([FromBody] DeliverableEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deliverable = new DELIVERABLE
            {
                GUID = entity.Guid,
                PROJECT_GUID = entity.ProjectGuid,
                AREA_NUMBER = entity.AreaNumber,
                DISCIPLINE = entity.Discipline,
                DOCUMENT_TYPE = entity.DocumentType,
                DEPARTMENT_ID = entity.DepartmentId,
                DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber,
                CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber,
                DOCUMENT_TITLE = entity.DocumentTitle,
                BUDGET_HOURS = entity.BudgetHours,
                VARIATION_HOURS = entity.VariationHours,
                TOTAL_COST = entity.TotalCost
            };

            var result = await _repository.CreateAsync(deliverable);
            return Created(MapToEntity(result));
        }

        public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] DeliverableEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != entity.Guid)
                return BadRequest("The ID in the URL must match the ID in the request body");

            try
            {
                var deliverable = new DELIVERABLE
                {
                    GUID = entity.Guid,
                    PROJECT_GUID = entity.ProjectGuid,
                    AREA_NUMBER = entity.AreaNumber,
                    DISCIPLINE = entity.Discipline,
                    DOCUMENT_TYPE = entity.DocumentType,
                    DEPARTMENT_ID = entity.DepartmentId,
                    DELIVERABLE_TYPE_ID = entity.DeliverableTypeId,
                    INTERNAL_DOCUMENT_NUMBER = entity.InternalDocumentNumber,
                    CLIENT_DOCUMENT_NUMBER = entity.ClientDocumentNumber,
                    DOCUMENT_TITLE = entity.DocumentTitle,
                    BUDGET_HOURS = entity.BudgetHours,
                    VARIATION_HOURS = entity.VariationHours,
                    TOTAL_COST = entity.TotalCost
                };

                var result = await _repository.UpdateAsync(deliverable);
                return Updated(MapToEntity(result));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Delete([FromRoute] Guid key, [FromBody] Guid deletedBy)
        {
            var result = await _repository.DeleteAsync(key, deletedBy);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Partially updates a deliverable
        /// </summary>
        /// <param name="key">The GUID of the deliverable to update</param>
        /// <param name="delta">The deliverable properties to update</param>
        /// <returns>The updated deliverable</returns>
        public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<DeliverableEntity> delta)
        {
            try
            {
                _logger?.LogInformation($"Received PATCH request for deliverable {key}");

                if (key == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid GUID", message = "The deliverable ID cannot be empty" });
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for deliverable {key}");
                    return BadRequest(new 
                    { 
                        error = "Update data cannot be null",
                        message = "The request body must contain valid properties to update."
                    });
                }

                // Get the existing deliverable
                var existingDeliverable = await _repository.GetByIdAsync(key);
                if (existingDeliverable == null)
                {
                    return NotFound(new { error = "Not Found", message = $"Deliverable with ID {key} was not found" });
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingDeliverable);
                delta.CopyChangedValues(updatedEntity);

                // Map back to DELIVERABLE entity
                var deliverableToUpdate = new DELIVERABLE
                {
                    GUID = updatedEntity.Guid,
                    PROJECT_GUID = updatedEntity.ProjectGuid,
                    AREA_NUMBER = updatedEntity.AreaNumber,
                    DISCIPLINE = updatedEntity.Discipline,
                    DOCUMENT_TYPE = updatedEntity.DocumentType,
                    DEPARTMENT_ID = updatedEntity.DepartmentId,
                    DELIVERABLE_TYPE_ID = updatedEntity.DeliverableTypeId,
                    INTERNAL_DOCUMENT_NUMBER = updatedEntity.InternalDocumentNumber,
                    CLIENT_DOCUMENT_NUMBER = updatedEntity.ClientDocumentNumber,
                    DOCUMENT_TITLE = updatedEntity.DocumentTitle,
                    BUDGET_HOURS = updatedEntity.BudgetHours,
                    VARIATION_HOURS = updatedEntity.VariationHours,
                    TOTAL_COST = updatedEntity.TotalCost
                };

                var result = await _repository.UpdateAsync(deliverableToUpdate);
                return Updated(MapToEntity(result));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating deliverable");
                return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
            }
        }

        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable)
        {
            // Extract client number and project number from the Project entity if available
            string clientNumber = deliverable.Project?.Client?.NUMBER ?? string.Empty;
            string projectNumber = deliverable.Project?.PROJECT_NUMBER ?? string.Empty;
            
            // Calculate derived fields
            string bookingCode = !string.IsNullOrEmpty(clientNumber) && 
                                !string.IsNullOrEmpty(projectNumber) && 
                                !string.IsNullOrEmpty(deliverable.AREA_NUMBER) && 
                                !string.IsNullOrEmpty(deliverable.DISCIPLINE)
                ? $"{clientNumber}-{projectNumber}-{deliverable.AREA_NUMBER}-{deliverable.DISCIPLINE}"
                : string.Empty; // Use empty string as fallback since BOOKING_CODE no longer exists
                
            // Always use the database-stored value for internal document number
            string internalDocumentNumber = deliverable.INTERNAL_DOCUMENT_NUMBER;
            
            decimal totalHours = deliverable.BUDGET_HOURS + deliverable.VARIATION_HOURS;
            
            return new DeliverableEntity
            {
                Guid = deliverable.GUID,
                ProjectGuid = deliverable.PROJECT_GUID,
                ClientNumber = clientNumber,
                ProjectNumber = projectNumber,
                AreaNumber = deliverable.AREA_NUMBER,
                Discipline = deliverable.DISCIPLINE,
                DocumentType = deliverable.DOCUMENT_TYPE,
                DepartmentId = deliverable.DEPARTMENT_ID,
                DeliverableTypeId = deliverable.DELIVERABLE_TYPE_ID,
                InternalDocumentNumber = internalDocumentNumber,
                ClientDocumentNumber = deliverable.CLIENT_DOCUMENT_NUMBER,
                DocumentTitle = deliverable.DOCUMENT_TITLE,
                BudgetHours = deliverable.BUDGET_HOURS,
                VariationHours = deliverable.VARIATION_HOURS,
                TotalHours = totalHours,
                TotalCost = deliverable.TOTAL_COST,
                BookingCode = bookingCode,
                Created = deliverable.CREATED,
                CreatedBy = deliverable.CREATEDBY,
                Updated = deliverable.UPDATED,
                UpdatedBy = deliverable.UPDATEDBY,
                Deleted = deliverable.DELETED,
                DeletedBy = deliverable.DELETEDBY,
                Project = deliverable.Project != null ? new ProjectEntity
                {
                    Guid = deliverable.Project.GUID,
                    ClientGuid = deliverable.Project.GUID_CLIENT,
                    ProjectNumber = deliverable.Project.PROJECT_NUMBER,
                    Name = deliverable.Project.NAME,
                    Client = deliverable.Project.Client != null ? new ClientEntity {
                        Guid = deliverable.Project.Client.GUID,
                        Number = deliverable.Project.Client.NUMBER,
                        Description = deliverable.Project.Client.DESCRIPTION
                    } : null
                } : null
            };
        }
    }
}
