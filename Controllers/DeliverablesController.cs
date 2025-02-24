using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class DeliverablesController : FourSPMODataController
    {
        private readonly IDeliverableRepository _repository;

        public DeliverablesController(IDeliverableRepository repository)
        {
            _repository = repository;
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
                TOTAL_HOURS = entity.TotalHours,
                TOTAL_COST = entity.TotalCost,
                BOOKING_CODE = entity.BookingCode
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
                    TOTAL_HOURS = entity.TotalHours,
                    TOTAL_COST = entity.TotalCost,
                    BOOKING_CODE = entity.BookingCode
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

        private static DeliverableEntity MapToEntity(DELIVERABLE deliverable)
        {
            return new DeliverableEntity
            {
                Guid = deliverable.GUID,
                ProjectGuid = deliverable.PROJECT_GUID,
                AreaNumber = deliverable.AREA_NUMBER,
                Discipline = deliverable.DISCIPLINE,
                DocumentType = deliverable.DOCUMENT_TYPE,
                DepartmentId = deliverable.DEPARTMENT_ID,
                DeliverableTypeId = deliverable.DELIVERABLE_TYPE_ID,
                InternalDocumentNumber = deliverable.INTERNAL_DOCUMENT_NUMBER,
                ClientDocumentNumber = deliverable.CLIENT_DOCUMENT_NUMBER,
                DocumentTitle = deliverable.DOCUMENT_TITLE,
                BudgetHours = deliverable.BUDGET_HOURS,
                VariationHours = deliverable.VARIATION_HOURS,
                TotalHours = deliverable.TOTAL_HOURS,
                TotalCost = deliverable.TOTAL_COST,
                BookingCode = deliverable.BOOKING_CODE,
                Created = deliverable.CREATED,
                CreatedBy = deliverable.CREATEDBY,
                Updated = deliverable.UPDATED,
                UpdatedBy = deliverable.UPDATEDBY,
                Deleted = deliverable.DELETED,
                DeletedBy = deliverable.DELETEDBY,
                Department = deliverable.Department != null ? new DepartmentEntity
                {
                    Guid = deliverable.Department.GUID,
                    Name = deliverable.Department.NAME,
                    Description = deliverable.Department.DESCRIPTION
                } : null,
                DeliverableType = deliverable.DeliverableType != null ? new DeliverableTypeEntity
                {
                    Guid = deliverable.DeliverableType.GUID,
                    Name = deliverable.DeliverableType.NAME,
                    Description = deliverable.DeliverableType.DESCRIPTION
                } : null,
                Project = deliverable.Project != null ? new ProjectEntity
                {
                    Guid = deliverable.Project.GUID,
                    ClientNumber = deliverable.Project.CLIENT_NUMBER,
                    ProjectNumber = deliverable.Project.PROJECT_NUMBER,
                    Name = deliverable.Project.NAME
                } : null
            };
        }
    }
}
