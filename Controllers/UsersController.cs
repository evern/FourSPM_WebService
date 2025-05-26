using AutoMapper;
using AutoMapper.AspNet.OData;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.OData.FourSPM;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FourSPM_WebService.Controllers;

[Authorize]
[ODataRouteComponent("odata/v1")]
public class UsersController : FourSPMODataController
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly FourSPMContext _context;

    public UsersController
    (
        IUserRepository userRepository, FourSPMContext context, IMapper mapper)
    {
        _userRepository = userRepository;
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    //[EnableQuery]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ODataQueryResponse<UserEntity>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(ODataQueryOptions<USER> options)
    {
        var query = await UserRepository.Query(_context)
            .GetQueryAsync(_mapper, options);

        return Ok(query);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserEntity), (int)HttpStatusCode.OK)]
    public IActionResult Get([FromRoute] Guid key)
    {
        return Ok(_userRepository.Query().FirstOrDefault(p => p.Guid == key));
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] UserEntity userEntity)
    {
        var result = await _userRepository.CreateUser(userEntity, CurrentUser.UserId);

        return GetResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] Guid key)
    {
        var result = await _userRepository.DeleteUser(key, CurrentUser.UserId);

        return GetResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Put(Guid key, [FromBody] UserEntity update)
    {
        var result = await _userRepository.UpdateUser(update, CurrentUser.UserId);

        return GetResult(result);
    }

    [HttpPatch]
    public async Task<IActionResult> Patch(Guid key, [FromBody] Delta<UserEntity> update)
    {
        var entity = await _userRepository.Query().FirstOrDefaultAsync(p => p.Guid == key);
        if (entity == null)
        {
            return NotFound();
        }

        update.Patch(entity);

        var result = await _userRepository.UpdateUser(entity, CurrentUser.UserId);
        return GetResult(result);
    }
}