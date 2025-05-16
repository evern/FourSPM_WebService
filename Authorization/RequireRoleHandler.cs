using FourSPM_WebService.Services;
using Microsoft.AspNetCore.Authorization;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Authorization handler for checking if a user is in a specific role
    /// </summary>
    public class RequireRoleHandler : AuthorizationHandler<RequireRoleRequirement>
    {
        private readonly ITokenValidationService _tokenService;
        private readonly ILogger<RequireRoleHandler> _logger;

        public RequireRoleHandler(ITokenValidationService tokenService, ILogger<RequireRoleHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if the current user has the required role or any of the alternative roles
        /// </summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequireRoleRequirement requirement)
        {
            // Get user roles from token
            var userRoles = _tokenService.GetUserRoles();
            _logger.LogDebug("Checking roles for requirement: {Role}", requirement.RoleName);

            // Check if the user has the required role
            if (userRoles.Any(r => string.Equals(r, requirement.RoleName, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                _logger.LogDebug("User has required role: {Role}", requirement.RoleName);
                return Task.CompletedTask;
            }

            // Check if the user has any of the alternative roles
            if (requirement.AlternativeRoles != null && requirement.AlternativeRoles.Length > 0)
            {
                foreach (var role in requirement.AlternativeRoles)
                {
                    if (userRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Succeed(requirement);
                        _logger.LogDebug("User has alternative role: {Role}", role);
                        return Task.CompletedTask;
                    }
                }
            }

            _logger.LogWarning("Authorization failed - user does not have required role: {Role}", requirement.RoleName);
            return Task.CompletedTask;
        }
    }
}
