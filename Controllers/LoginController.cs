using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Controllers.Login.Controller;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class LoginController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly FourSPMContext _context;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        IAuthService authService,
        FourSPMContext context,
        ILogger<LoginController> logger)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.USERs
                .Where(u => u.USERNAME == request.Email && u.DELETED == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Empty password attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            if (!_authService.VerifyPassword(request.Password, user.PASSWORD))
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Email);
                return Unauthorized("Invalid username or password");
            }

            var token = _authService.GenerateJwtToken(user);

            // Set the token as a cookie with the following attributes:
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true, // Prevents JavaScript access to the cookie for security reasons
                Secure = true,   // Ensures the cookie is only sent over HTTPS connections
                SameSite = SameSiteMode.None, // Allows the cookie to be sent with cross-origin requests
                Expires = DateTime.UtcNow.AddDays(7), // Sets the cookie to expire in 7 days
                Path = "/", // The cookie is valid for all paths on the domain
            });

            return Ok(new 
            { 
                token,
                user = new
                {
                    user.GUID,
                    user.USERNAME,
                    user.FIRST_NAME,
                    user.LAST_NAME
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }
}