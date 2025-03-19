using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OData.Formatter;
using Newtonsoft.Json;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DeliverablesController : FourSPMODataController
    {
        private readonly IDeliverableRepository _repository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<DeliverablesController> _logger;

        public DeliverablesController(IDeliverableRepository repository, IProjectRepository projectRepository, ILogger<DeliverablesController> logger)
        {
            _repository = repository;
            _projectRepository = projectRepository;
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

        public async Task<IActionResult> Post([FromBody] DeliverableEntity entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deliverable = new DELIVERABLE
            {
                GUID = entity.Guid,
                GUID_PROJECT = entity.ProjectGuid,
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
                    GUID_PROJECT = entity.ProjectGuid,
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
                    return BadRequest("Invalid GUID - The deliverable ID cannot be empty");
                }

                if (delta == null)
                {
                    _logger?.LogWarning($"Update data is null for deliverable {key}");
                    return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
                }

                // Get the existing deliverable
                var existingDeliverable = await _repository.GetByIdAsync(key);
                if (existingDeliverable == null)
                {
                    return NotFound("Deliverable with ID " + key + " was not found");
                }

                // Create a copy of the entity to track changes
                var updatedEntity = MapToEntity(existingDeliverable);
                delta.CopyChangedValues(updatedEntity);

                // Map back to DELIVERABLE entity
                var deliverableToUpdate = new DELIVERABLE
                {
                    GUID = updatedEntity.Guid,
                    GUID_PROJECT = updatedEntity.ProjectGuid,
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
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }

        // Add a new action to suggest an internal document number
        [HttpGet("/odata/v1/Deliverables/SuggestInternalDocumentNumber")]
        public async Task<IActionResult> SuggestInternalDocumentNumber([FromQuery] Guid projectGuid, [FromQuery] string areaNumber, [FromQuery] string discipline, [FromQuery] string documentType, [FromQuery] string deliverableTypeId)
        {
            try
            {
                _logger?.LogInformation($"Generating suggested internal document number for project {projectGuid}");
                
                // Get the project details to get client and project numbers
                var project = await _projectRepository.GetProjectWithClientAsync(projectGuid);
                
                if (project == null)
                {
                    return NotFound($"Project with ID {projectGuid} not found");
                }

                // Get client and project numbers
                string clientNumber = project.Client?.NUMBER ?? string.Empty;
                string projectNumber = project.PROJECT_NUMBER ?? string.Empty;
                
                if (string.IsNullOrEmpty(clientNumber) || string.IsNullOrEmpty(projectNumber))
                {
                    return BadRequest("Client number or project number is missing from the project");
                }
                
                // Determine the format based on deliverable type
                // For "Deliverable" type: Client-Project-Area-Discipline-DocumentType-SequentialNumber
                // For other types: Client-Project-Discipline-DocumentType-SequentialNumber
                string baseFormat;
                if (deliverableTypeId == "Deliverable")
                {
                    if (string.IsNullOrEmpty(areaNumber))
                    {
                        return BadRequest("Area number is required for Deliverable type");
                    }
                    baseFormat = $"{clientNumber}-{projectNumber}-{areaNumber}";
                }
                else
                {
                    baseFormat = $"{clientNumber}-{projectNumber}";
                }
                
                // Add discipline and document type if provided
                if (!string.IsNullOrEmpty(discipline))
                {
                    baseFormat += $"-{discipline}";
                }
                
                if (!string.IsNullOrEmpty(documentType))
                {
                    baseFormat += $"-{documentType}";
                }
                
                // Find the highest sequence number for documents with this format
                var existingDeliverables = await _repository.GetDeliverablesByNumberPatternAsync(projectGuid, baseFormat);
                
                int nextSequence = 1;
                
                if (existingDeliverables.Any())
                {
                    foreach (var deliverable in existingDeliverables)
                    {
                        if (!string.IsNullOrEmpty(deliverable.INTERNAL_DOCUMENT_NUMBER))
                        {
                            var parts = deliverable.INTERNAL_DOCUMENT_NUMBER.Split('-');
                            if (parts.Length > 0)
                            {
                                var lastPart = parts[parts.Length - 1];
                                if (int.TryParse(lastPart, out int seq) && seq >= nextSequence)
                                {
                                    nextSequence = seq + 1;
                                }
                            }
                        }
                    }
                }
                
                // Format sequence as 3 digits (001, 002, etc.)
                string sequenceNumber = nextSequence.ToString().PadLeft(3, '0');
                
                // Build the final suggested number
                string suggestedNumber = $"{baseFormat}-{sequenceNumber}";
                
                return Ok(new { suggestedNumber });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating suggested internal document number");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
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
            
            // Calculate progress-related values
            var validProgressItems = deliverable.ProgressItems
                .Where(p => p.DELETED == null)
                .ToList();
            
            // Get the current period - take the max period from progress items or default to 1
            int currentPeriod = validProgressItems.Any() ? validProgressItems.Max(p => p.PERIOD) : 1;
            
            // Calculate total percentage earnt based on the units reported for the deliverable
            decimal totalPercentageEarnt = 0;
            if (validProgressItems.Any() && totalHours > 0)
            {
                // For total percentage, we look at the sum of all units reported
                decimal totalUnits = validProgressItems.Sum(p => p.UNITS);
                totalPercentageEarnt = totalUnits / totalHours;
            }
            
            // Calculate total earnt hours
            decimal totalEarntHours = totalHours * totalPercentageEarnt;
            
            // Calculate period percentage based on units in the current period's progress record
            decimal periodPercentageEarnt = 0;
            if (totalHours > 0) // Avoid division by zero
            {
                var currentPeriodProgress = validProgressItems
                    .FirstOrDefault(p => p.PERIOD == currentPeriod);
                
                if (currentPeriodProgress != null)
                {
                    periodPercentageEarnt = currentPeriodProgress.UNITS / totalHours;
                }
            }
            
            decimal periodEarntHours = totalHours * periodPercentageEarnt;
            
            return new DeliverableEntity
            {
                Guid = deliverable.GUID,
                ProjectGuid = deliverable.GUID_PROJECT,
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
                TotalPercentageEarnt = totalPercentageEarnt,
                TotalEarntHours = totalEarntHours,
                PeriodPercentageEarnt = periodPercentageEarnt,
                PeriodEarntHours = periodEarntHours,
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
                } : null,
                ProgressItems = validProgressItems.Select(p => new ProgressEntity
                {
                    Guid = p.GUID,
                    DeliverableGuid = p.GUID_DELIVERABLE,
                    Period = p.PERIOD,
                    Units = p.UNITS,
                    Created = p.CREATED,
                    CreatedBy = p.CREATEDBY,
                    Updated = p.UPDATED,
                    UpdatedBy = p.UPDATEDBY,
                    Deleted = p.DELETED,
                    DeletedBy = p.DELETEDBY
                }).ToList(),
                DeliverableGate = deliverable.DeliverableGate != null ? new DeliverableGateEntity
                {
                    Guid = deliverable.DeliverableGate.GUID,
                    Name = deliverable.DeliverableGate.NAME,
                    MaxPercentage = deliverable.DeliverableGate.MAX_PERCENTAGE,
                    AutoPercentage = deliverable.DeliverableGate.AUTO_PERCENTAGE
                } : null
            };
        }
    }
}
