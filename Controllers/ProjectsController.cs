using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.Authorization;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.JsonPatch;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Shared;
using FourSPM_WebService.Attributes;
using FourSPM_WebService.Data.Constants;

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class ProjectsController : FourSPMODataController
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ProjectsController> _logger;
    private readonly FourSPMContext _context;

    public ProjectsController(IProjectRepository projectRepository, ILogger<ProjectsController> logger, FourSPMContext context)
    {
        _projectRepository = projectRepository;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Retrieves all projects
    /// </summary>
    /// <returns>A list of projects</returns>
    [EnableQuery]
    public async Task<IActionResult> Get()
    {
        var projects = await _projectRepository.GetAllAsync();
        var entities = projects.Select(p => MapToEntity(p));
        return Ok(entities);
    }

    /// <summary>
    /// Retrieves a project by its GUID
    /// </summary>
    /// <param name="key">The GUID of the project to retrieve</param>
    /// <returns>The project with the specified GUID</returns>
    [EnableQuery]
    [RequirePermission(PermissionConstants.ProjectsView)]
    public async Task<IActionResult> Get([FromODataUri] Guid key)
    {
        var project = await _projectRepository.GetByIdAsync(key);
        if (project == null)
            return NotFound();

        return Ok(MapToEntity(project));
    }

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="project">The project to create</param>
    /// <returns>The created project</returns>
    [RequirePermission(PermissionConstants.ProjectsEdit)]
    public async Task<IActionResult> Post([FromBody] ProjectEntity entity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        // Project numbers no longer need to be unique - requirement changed

        var project = new PROJECT
        {
            GUID = entity.Guid,
            GUID_CLIENT = entity.ClientGuid,
            PROJECT_NUMBER = entity.ProjectNumber,
            NAME = entity.Name,
            PURCHASE_ORDER_NUMBER = entity.PurchaseOrderNumber,
            PROJECT_STATUS = entity.ProjectStatus,
            PROGRESS_START = entity.ProgressStart,
            CONTACT_NAME = entity.ContactName,
            CONTACT_NUMBER = entity.ContactNumber,
            CONTACT_EMAIL = entity.ContactEmail
        };

        // Create the project using the repository
        var result = await _projectRepository.CreateAsync(project, CurrentUser.UserId);
        return Created(MapToEntity(result));
    }

    /// <summary>
    /// Deletes a project by its GUID
    /// </summary>
    /// <param name="key">The GUID of the project to delete</param>
    /// <returns>A success message if the project was deleted successfully</returns>
    [RequirePermission(PermissionConstants.ProjectsEdit)]
    public async Task<IActionResult> Delete([FromRoute] Guid key)
    {
        var result = await _projectRepository.DeleteAsync(key, CurrentUser.UserId ?? Guid.Empty);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates a project
    /// </summary>
    /// <param name="key">The GUID of the project to update</param>
    /// <param name="entity">The project properties to update</param>
    /// <returns>The updated project</returns>
    [RequirePermission(PermissionConstants.ProjectsEdit)]
    public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] ProjectEntity entity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (key != entity.Guid)
            return BadRequest("The ID in the URL must match the ID in the request body");

        try
        {
            // Project numbers no longer need to be unique - requirement changed
            
            var project = new PROJECT
            {
                GUID = entity.Guid,
                GUID_CLIENT = entity.ClientGuid,
                PROJECT_NUMBER = entity.ProjectNumber,
                NAME = entity.Name,
                PURCHASE_ORDER_NUMBER = entity.PurchaseOrderNumber,
                PROJECT_STATUS = entity.ProjectStatus,
                PROGRESS_START = entity.ProgressStart,
                CONTACT_NAME = entity.ContactName,
                CONTACT_NUMBER = entity.ContactNumber,
                CONTACT_EMAIL = entity.ContactEmail
            };

            var result = await _projectRepository.UpdateAsync(project, CurrentUser.UserId);
            return Updated(MapToEntity(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Partially updates a project
    /// </summary>
    /// <param name="key">The GUID of the project to update</param>
    /// <param name="delta">The project properties to update</param>
    /// <returns>The updated project</returns>
    [RequirePermission(PermissionConstants.ProjectsEdit)]
    public async Task<IActionResult> Patch([FromODataUri] Guid key, [FromBody] Delta<ProjectEntity> delta)
    {
        try
        {
            _logger?.LogInformation($"Received PATCH request for project {key}");

            if (key == Guid.Empty)
            {
                return BadRequest("Invalid GUID - The project ID cannot be empty");
            }

            if (delta == null)
            {
                _logger?.LogWarning($"Update data is null for project {key}");
                return BadRequest("Update data cannot be null. The request body must contain valid properties to update.");
            }

            // Get the existing project
            var existingProject = await _projectRepository.GetByIdAsync(key);
            if (existingProject == null)
            {
                return NotFound(new { error = "Not Found", message = $"Project with ID {key} was not found" });
            }

            // Get the changed property names
            var changedProperties = delta.GetChangedPropertyNames();
            _logger?.LogInformation($"Changed properties: {string.Join(", ", changedProperties)}");
            
            // Log if client_* fields are being sent (but we won't use them)
            bool hasClientContactFields = changedProperties.Any(p => 
                p.StartsWith("client_"));
            if (hasClientContactFields)
            {
                _logger?.LogWarning($"Client contact fields were included in Project patch request but will be ignored. " +
                    $"Please use the Clients endpoint to update client information.");
            }

            // Create a copy of the entity to track changes
            var updatedEntity = MapToEntity(existingProject);
            delta.CopyChangedValues(updatedEntity);
            
            // Project numbers no longer need to be unique - requirement changed

            // Map project fields back to PROJECT entity
            existingProject.GUID_CLIENT = updatedEntity.ClientGuid;
            existingProject.PROJECT_NUMBER = updatedEntity.ProjectNumber;
            existingProject.NAME = updatedEntity.Name;
            existingProject.PURCHASE_ORDER_NUMBER = updatedEntity.PurchaseOrderNumber;
            existingProject.PROJECT_STATUS = updatedEntity.ProjectStatus;
            existingProject.PROGRESS_START = updatedEntity.ProgressStart;
            existingProject.CONTACT_NAME = updatedEntity.ContactName;
            existingProject.CONTACT_NUMBER = updatedEntity.ContactNumber;
            existingProject.CONTACT_EMAIL = updatedEntity.ContactEmail;

            // Update the project
            var result = await _projectRepository.UpdateAsync(existingProject, CurrentUser.UserId);

            // Return the updated project with client details included
            return Updated(MapToEntity(result));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating project");
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets projects with their client data explicitly included
    /// </summary>
    /// <returns>Projects with client data</returns>
    [HttpGet("/odata/v1/Projects/GetWithClientData")]
    [RequirePermission(PermissionConstants.ProjectsView)]
    public async Task<IActionResult> GetWithClientData()
    {
        try
        {
            _logger?.LogInformation("Getting projects with client data");
            
            // Get all projects with eager loading of client data
            var projects = await _projectRepository.GetAllWithClientsAsync();
            
            // Map to entities with client data included
            var entities = projects.Select(p => MapToEntity(p)).ToList();
            
            // Use the ODataResponse class for proper OData format
            var response = new ODataResponse<ProjectEntity>
            {
                Value = entities,
                Count = entities.Count()
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting projects with client data");
            return StatusCode(500, "Internal Server Error - " + ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific project with its client data explicitly included
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <returns>Project with client data</returns>
    [HttpGet("/odata/v1/Projects/GetWithClientData/{projectId}")]
    [RequirePermission(PermissionConstants.ProjectsView)]
    public async Task<IActionResult> GetWithClientData(Guid projectId)
    {
        try
        {
            _logger?.LogInformation($"Getting project {projectId} with client data");
            
            // Get project with eager loading of client data
            var project = await _projectRepository.GetProjectWithClientAsync(projectId);
            
            if (project == null)
                return NotFound();
                
            // Map to entity with client data included
            var entity = MapToEntity(project);
            
            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting project {projectId} with client data");
            return StatusCode(500, "Internal Server Error - " + ex.Message);
        }
    }

    /// <summary>
    /// Maps a PROJECT database entity to a ProjectEntity OData entity
    /// </summary>
    /// <param name="project">The PROJECT database entity</param>
    /// <returns>A ProjectEntity OData entity</returns>
    private ProjectEntity MapToEntity(PROJECT project)
    {
        // Create the base project entity
        var projectEntity = new ProjectEntity
        {
            Guid = project.GUID,
            ClientGuid = project.GUID_CLIENT,
            ProjectNumber = project.PROJECT_NUMBER,
            Name = project.NAME,
            PurchaseOrderNumber = project.PURCHASE_ORDER_NUMBER,
            ProjectStatus = project.PROJECT_STATUS,
            ProgressStart = project.PROGRESS_START,
            ContactName = project.CONTACT_NAME,
            ContactNumber = project.CONTACT_NUMBER,
            ContactEmail = project.CONTACT_EMAIL,
            Created = project.CREATED,
            CreatedBy = project.CREATEDBY,
            Updated = project.UPDATED,
            UpdatedBy = project.UPDATEDBY,
            Deleted = project.DELETED,
            DeletedBy = project.DELETEDBY
        };

        // If there's a client associated with the project, include its details
        if (project.Client != null)
        {
            projectEntity.Client = new ClientEntity
            {
                Guid = project.Client.GUID,
                Number = project.Client.NUMBER,
                Description = project.Client.DESCRIPTION
            };
        }

        return projectEntity;
    }
    
    /// <summary>
    /// Checks if a project number is unique in the database
    /// </summary>
    /// <param name="projectNumber">The project number to check</param>
    /// <param name="excludeProjectGuid">Optional GUID of the project to exclude from the check (for updates)</param>
    /// <returns>True if the project number is unique, false otherwise</returns>
    private async Task<bool> IsProjectNumberUnique(string projectNumber, Guid? excludeProjectGuid)
    {
        if (string.IsNullOrEmpty(projectNumber))
            return true; // Empty project numbers are handled by model validation
            
        var query = _context.PROJECTs
            .Where(p => p.PROJECT_NUMBER == projectNumber && p.DELETED == null);
            
        // If we're updating an existing project, exclude it from the uniqueness check
        if (excludeProjectGuid.HasValue)
        {
            query = query.Where(p => p.GUID != excludeProjectGuid.Value);
        }
        
        return await query.CountAsync() == 0;
    }
}