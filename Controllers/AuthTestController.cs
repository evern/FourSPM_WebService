using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Security.Claims;

namespace FourSPM_WebService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : AuthenticatedControllerBase
    {
        private readonly ILogger<AuthTestController> _logger;

        public AuthTestController(ILogger<AuthTestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "This is a public endpoint. No authentication required." });
        }

        [HttpGet("user")]
        [RequiredScope("Application.User")]
        public IActionResult UserEndpoint()
        {
            var userInfo = new
            {
                ObjectId = GetUserObjectId(),
                Email = GetUserEmail(),
                DisplayName = GetUserDisplayName(),
                Roles = GetUserRoles().ToList()
            };

            _logger.LogInformation("User authenticated: {Email}", userInfo.Email);
            
            return Ok(new
            {
                message = "You're authenticated as a user.",
                userInfo
            });
        }

        [HttpGet("admin")]
        [RequiredScope("Application.Admin")]
        public IActionResult AdminEndpoint()
        {
            var userInfo = new
            {
                ObjectId = GetUserObjectId(),
                Email = GetUserEmail(),
                DisplayName = GetUserDisplayName(),
                Roles = GetUserRoles().ToList(),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            _logger.LogInformation("Admin authenticated: {Email}", userInfo.Email);
            
            return Ok(new
            {
                message = "You're authenticated as an admin.",
                userInfo
            });
        }
    }
}
