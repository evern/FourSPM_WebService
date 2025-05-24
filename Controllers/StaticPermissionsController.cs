using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourSPM_WebService.Data.Constants;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Controllers
{
    [Authorize]
    [ODataRouteComponent("odata/v1")]
    public class StaticPermissionsController : FourSPMODataController
    {
        private readonly ILogger<StaticPermissionsController> _logger;

        public StaticPermissionsController(ILogger<StaticPermissionsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets all available static permissions in the system
        /// </summary>
        [EnableQuery]
        public IActionResult Get()
        {
            try
            {
                var permissions = PermissionConstants.GetStaticPermissions();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving static permissions");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
