using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using System.Net;

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class ProjectsController : FourSPMODataController
{
    private readonly IProjectRepository _projectRepository;

    public ProjectsController(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [HttpGet]
    [EnableQuery]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ODataQueryResponse<ProjectEntity>), (int)HttpStatusCode.OK)]
    public IActionResult Get()
    {
        return Ok(_projectRepository.ProjectQuery());
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProjectEntity), (int)HttpStatusCode.OK)]
    public IActionResult Get([FromRoute] Guid key)
    {
        return Ok(_projectRepository.ProjectQuery().FirstOrDefault(p => p.Guid == key));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ProjectEntity project)
    {
        var result = await _projectRepository.CreateProject(project);

        return GetResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] Guid key)
    {
        var result = await _projectRepository.DeleteProject(key);

        return GetResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Put(Guid key, [FromBody] ProjectEntity update)
    {
        var result = await _projectRepository.UpdateProject(update);

        return GetResult(result);
    }

    [HttpPatch]
    public async Task<IActionResult> Patch(Guid key, [FromBody] Delta<ProjectEntity> update)
    {
        var result = await _projectRepository.UpdateProjectByKey(
            key,
            entity => update.Patch(entity)
        );

        return GetResult(result);
    }
}