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

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class ProjectsController : FourSPMODataController
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectRepository projectRepository, ILogger<ProjectsController> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all projects
    /// </summary>
    /// <returns>A list of projects</returns>
    [EnableQuery]
    public IActionResult Get()
    {
        try
        {
            return Ok(_projectRepository.ProjectQuery());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a project by its GUID
    /// </summary>
    /// <param name="key">The GUID of the project to retrieve</param>
    /// <returns>The project with the specified GUID</returns>
    [EnableQuery]
    public IActionResult Get([FromRoute] Guid key)
    {
        try
        {
            _logger.LogInformation($"Fetching project with GUID: {key}");
            var project = _projectRepository.ProjectQuery().FirstOrDefault(p => p.Guid == key);
            
            if (project == null)
            {
                _logger.LogWarning($"Project not found with GUID: {key}");
                return NotFound($"Project with GUID {key} not found");
            }

            _logger.LogInformation($"Successfully retrieved project: {project.ProjectNumber}");
            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving project with GUID: {key}");
            return StatusCode(500, "Internal server error occurred while retrieving the project");
        }
    }

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="project">The project to create</param>
    /// <returns>The created project</returns>
    public async Task<IActionResult> Post([FromBody] ProjectEntity project)
    {
        var result = await _projectRepository.CreateProject(project);
        return GetResult(result);
    }

    /// <summary>
    /// Deletes a project by its GUID
    /// </summary>
    /// <param name="key">The GUID of the project to delete</param>
    /// <returns>A success message if the project was deleted successfully</returns>
    public async Task<IActionResult> Delete([FromRoute] Guid key)
    {
        var result = await _projectRepository.DeleteProject(key);
        return GetResult(result);
    }

    /// <summary>
    /// Updates a project
    /// </summary>
    /// <param name="key">The GUID of the project to update</param>
    /// <param name="update">The project properties to update</param>
    /// <returns>The updated project</returns>
    public async Task<IActionResult> Put([FromRoute] Guid key, [FromBody] ProjectEntity update)
    {
        if (key != update.Guid)
        {
            return BadRequest(new { error = "Route key and project GUID do not match" });
        }
        var result = await _projectRepository.UpdateProject(update);
        return GetResult(result);
    }

    /// <summary>
    /// Partially updates a project
    /// </summary>
    /// <param name="key">The GUID of the project to update</param>
    /// <param name="delta">The project properties to update</param>
    /// <returns>The updated project</returns>
    public async Task<IActionResult> Patch([FromRoute] Guid key, [FromBody] Delta<ProjectEntity> delta)
    {
        try
        {
            _logger?.LogInformation($"Received PATCH request for project {key}");

            if (key == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid GUID", message = "The project ID cannot be empty" });
            }

            if (delta == null)
            {
                _logger?.LogWarning($"Update data is null for project {key}");
                return BadRequest(new 
                { 
                    error = "Update data cannot be null",
                    message = "The request body must contain valid properties to update."
                });
            }

            // Get the project to update first
            var entity = await _projectRepository.ProjectQuery()
                .FirstOrDefaultAsync(x => x.Guid == key);

            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Project with ID {key} was not found" });
            }

            // Create a copy of the entity to track changes
            var updatedEntity = new ProjectEntity();
            delta.CopyChangedValues(updatedEntity);

            // Save the changes
            var updateResult = await _projectRepository.UpdateProjectByKey(
                key,
                e => {
                    foreach (var propName in delta.GetChangedPropertyNames())
                    {
                        var prop = typeof(ProjectEntity).GetProperty(propName);
                        if (prop != null)
                        {
                            var value = prop.GetValue(updatedEntity);
                            prop.SetValue(e, value);
                        }
                    }
                }
            );

            return GetResult(updateResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating project");
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }
}