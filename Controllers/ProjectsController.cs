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
    public async Task<IActionResult> Post([FromBody] ProjectEntity entity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        // Check if project number is unique
        if (!await IsProjectNumberUnique(entity.ProjectNumber, null))
        {
            return BadRequest($"A project with number '{entity.ProjectNumber}' already exists.");
        }

        var project = new PROJECT
        {
            GUID = entity.Guid,
            GUID_CLIENT = entity.ClientGuid,
            PROJECT_NUMBER = entity.ProjectNumber,
            NAME = entity.Name,
            PURCHASE_ORDER_NUMBER = entity.PurchaseOrderNumber,
            PROJECT_STATUS = entity.ProjectStatus,
            PROGRESS_START = entity.ProgressStart
        };

        // Create the project using the repository
        var result = await _projectRepository.CreateAsync(project);
        return Created(MapToEntity(result));
    }

    /// <summary>
    /// Deletes a project by its GUID
    /// </summary>
    /// <param name="key">The GUID of the project to delete</param>
    /// <returns>A success message if the project was deleted successfully</returns>
    public async Task<IActionResult> Delete([FromRoute] Guid key, [FromBody] Guid deletedBy)
    {
        var result = await _projectRepository.DeleteAsync(key, deletedBy);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates a project
    /// </summary>
    /// <param name="key">The GUID of the project to update</param>
    /// <param name="entity">The project properties to update</param>
    /// <returns>The updated project</returns>
    public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] ProjectEntity entity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (key != entity.Guid)
            return BadRequest("The ID in the URL must match the ID in the request body");

        try
        {
            // Check if project number is unique (excluding the current project)
            if (!await IsProjectNumberUnique(entity.ProjectNumber, key))
            {
                return BadRequest($"A project with number '{entity.ProjectNumber}' already exists.");
            }
            
            var project = new PROJECT
            {
                GUID = entity.Guid,
                GUID_CLIENT = entity.ClientGuid,
                PROJECT_NUMBER = entity.ProjectNumber,
                NAME = entity.Name,
                PURCHASE_ORDER_NUMBER = entity.PurchaseOrderNumber,
                PROJECT_STATUS = entity.ProjectStatus,
                PROGRESS_START = entity.ProgressStart
            };

            var result = await _projectRepository.UpdateAsync(project);
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

            // Create a copy of the entity to track changes
            var updatedEntity = MapToEntity(existingProject);
            delta.CopyChangedValues(updatedEntity);
            
            // Check if project number is being changed and is unique (if it's being updated)
            if (delta.GetChangedPropertyNames().Contains("ProjectNumber") && 
                !await IsProjectNumberUnique(updatedEntity.ProjectNumber, key))
            {
                return BadRequest($"A project with number '{updatedEntity.ProjectNumber}' already exists.");
            }

            // Map back to PROJECT entity
            var projectToUpdate = new PROJECT
            {
                GUID = updatedEntity.Guid,
                GUID_CLIENT = updatedEntity.ClientGuid,
                PROJECT_NUMBER = updatedEntity.ProjectNumber,
                NAME = updatedEntity.Name,
                PURCHASE_ORDER_NUMBER = updatedEntity.PurchaseOrderNumber,
                PROJECT_STATUS = updatedEntity.ProjectStatus,
                PROGRESS_START = updatedEntity.ProgressStart,
                CREATED = existingProject.CREATED,
                CREATEDBY = existingProject.CREATEDBY,
                UPDATED = existingProject.UPDATED,
                UPDATEDBY = existingProject.UPDATEDBY,
                DELETED = existingProject.DELETED,
                DELETEDBY = existingProject.DELETEDBY
            };

            var result = await _projectRepository.UpdateAsync(projectToUpdate);
            return Updated(MapToEntity(result));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating project");
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    /// <summary>
    /// Maps a PROJECT database entity to a ProjectEntity OData entity
    /// </summary>
    /// <param name="project">The PROJECT database entity</param>
    /// <returns>A ProjectEntity OData entity</returns>
    private ProjectEntity MapToEntity(PROJECT project)
    {
        return new ProjectEntity
        {
            Guid = project.GUID,
            ClientGuid = project.GUID_CLIENT,
            ProjectNumber = project.PROJECT_NUMBER,
            Name = project.NAME,
            PurchaseOrderNumber = project.PURCHASE_ORDER_NUMBER,
            ProjectStatus = project.PROJECT_STATUS,
            ProgressStart = project.PROGRESS_START,
            Created = project.CREATED,
            CreatedBy = project.CREATEDBY,
            Updated = project.UPDATED,
            UpdatedBy = project.UPDATEDBY,
            Deleted = project.DELETED,
            DeletedBy = project.DELETEDBY,
            
            // Use the navigation property instead of explicit query
            ClientContactName = project.Client?.CLIENT_CONTACT_NAME,
            ClientContactNumber = project.Client?.CLIENT_CONTACT_NUMBER,
            ClientContactEmail = project.Client?.CLIENT_CONTACT_EMAIL
        };
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