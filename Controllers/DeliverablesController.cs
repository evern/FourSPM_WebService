using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OData.Formatter;
using FourSPM_WebService.Models.Shared;

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
                existingDeliverable.GUID_PROJECT = updatedEntity.ProjectGuid;
                existingDeliverable.AREA_NUMBER = updatedEntity.AreaNumber;
                existingDeliverable.DISCIPLINE = updatedEntity.Discipline;
                existingDeliverable.DOCUMENT_TYPE = updatedEntity.DocumentType;
                existingDeliverable.DEPARTMENT_ID = updatedEntity.DepartmentId;
                existingDeliverable.DELIVERABLE_TYPE_ID = updatedEntity.DeliverableTypeId;
                existingDeliverable.GUID_DELIVERABLE_GATE = updatedEntity.DeliverableGateGuid;
                existingDeliverable.INTERNAL_DOCUMENT_NUMBER = updatedEntity.InternalDocumentNumber;
                existingDeliverable.CLIENT_DOCUMENT_NUMBER = updatedEntity.ClientDocumentNumber;
                existingDeliverable.DOCUMENT_TITLE = updatedEntity.DocumentTitle;
                existingDeliverable.BUDGET_HOURS = updatedEntity.BudgetHours;
                existingDeliverable.VARIATION_HOURS = updatedEntity.VariationHours;
                existingDeliverable.TOTAL_COST = updatedEntity.TotalCost;

                var result = await _repository.UpdateAsync(existingDeliverable);
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

        // Add a new endpoint to get deliverables with progress percentages for a specific project and period
        [HttpGet("/odata/v1/Deliverables/GetWithProgressPercentages")]
        public async Task<IActionResult> GetWithProgressPercentages([FromQuery] Guid projectGuid, [FromQuery] int period)
        {
            try
            {
                _logger?.LogInformation($"Getting deliverables with progress percentages for project {projectGuid}, period {period}");
                
                var deliverables = await _repository.GetByProjectIdAndPeriodAsync(projectGuid, period);
                
                var entities = deliverables.Select(d => {
                    var entity = MapToEntity(d, period);
                    // Calculate previousPeriodEarnedPercentage and futurePeriodEarnedPercentage
                    CalculateProgressPercentages(entity, period);
                    return entity;
                }).ToList();
                
                // Use the existing ODataResponse class which properly formats the JSON with "@odata.count" property
                var response = new ODataResponse<DeliverableEntity>
                {
                    Value = entities,
                    Count = entities.Count
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting deliverables with progress percentages");
                return StatusCode(500, "Internal Server Error - " + ex.Message);
            }
        }
        
        private static void CalculateProgressPercentages(DeliverableEntity entity, int currentPeriod)
        {
            // Default values
            entity.PreviousPeriodEarnedPercentage = 0;
            entity.FuturePeriodEarnedPercentage = 1.0m;
            
            if (entity.ProgressItems == null || !entity.ProgressItems.Any() || entity.TotalHours <= 0)
            {
                return;
            }

            // Calculate previous period earned percentage
            var previousPeriodItems = entity.ProgressItems
                .Where(item => item.Period < currentPeriod && item.Deleted == null)
                .ToList();
            
            if (previousPeriodItems.Any())
            {
                // Get the most recent previous period
                var maxPreviousPeriod = previousPeriodItems.Max(item => item.Period);
                var previousPeriodItem = previousPeriodItems.FirstOrDefault(item => item.Period == maxPreviousPeriod);
                
                if (previousPeriodItem != null)
                {
                    entity.PreviousPeriodEarnedPercentage = previousPeriodItem.Units / entity.TotalHours;
                }
            }
            
            // Calculate future period earned percentage
            var futurePeriodItems = entity.ProgressItems
                .Where(item => item.Period > currentPeriod && item.Deleted == null)
                .ToList();
            
            if (futurePeriodItems.Any())
            {
                // Get the earliest future period
                var minFuturePeriod = futurePeriodItems.Min(item => item.Period);
                var futurePeriodItem = futurePeriodItems.FirstOrDefault(item => item.Period == minFuturePeriod);
                
                if (futurePeriodItem != null)
                {
                    entity.FuturePeriodEarnedPercentage = futurePeriodItem.Units / entity.TotalHours;
                }
            }
        }

        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable)
        {
            return MapToEntity(deliverable, 0); // Use 0 as default period when not calculating percentages
        }

        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable, int period)
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
                DeliverableGateGuid = deliverable.GUID_DELIVERABLE_GATE,
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
